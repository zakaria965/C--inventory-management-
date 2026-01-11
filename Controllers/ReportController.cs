using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Filters;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace InventoryManagementSystem.Controllers
{
    [RequireNotUserRole]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<IActionResult> Index()
        {
            // Get data for KPI cards and charts
            var totalProducts = await _context.Products.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var totalEmployees = await _context.Employees.CountAsync();
            var totalSuppliers = await _context.Suppliers.CountAsync();
            var totalInventoryValue = await _context.Products.SumAsync(p => p.QuantityInStock * p.SellingPrice);
            var totalOutgoings = await _context.Outgoings.CountAsync();
            var pendingOrders = await _context.Orders.Where(o => o.Status == "Pending").CountAsync();
            var lowStockProducts = await _context.Products
                .Where(p => p.MinimumStockLevel.HasValue && p.QuantityInStock <= p.MinimumStockLevel.Value)
                .CountAsync();

            // Order status distribution
            var orderStatusData = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            // Product category distribution
            var categoryData = await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .GroupBy(p => p.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            // Outgoing reasons distribution
            var outgoingReasons = await _context.Outgoings
                .Where(o => !string.IsNullOrEmpty(o.Reason))
                .GroupBy(o => o.Reason)
                .Select(g => new { Reason = g.Key, Count = g.Count() })
                .ToListAsync();

            // Monthly orders trend (last 6 months)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var monthlyOrdersData = await _context.Orders
                .Where(o => o.OrderDate >= sixMonthsAgo)
                .Select(o => new { 
                    Year = o.OrderDate.Year,
                    Month = o.OrderDate.Month,
                    TotalAmount = o.TotalAmount
                })
                .ToListAsync();

            var monthlyOrders = monthlyOrdersData
                .GroupBy(o => new { Year = o.Year, Month = o.Month })
                .Select(g => new { 
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Count = g.Count(),
                    Total = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Month)
                .ToList();

            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.TotalSuppliers = totalSuppliers;
            ViewBag.TotalInventoryValue = totalInventoryValue;
            ViewBag.TotalOutgoings = totalOutgoings;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.LowStockProducts = lowStockProducts;
            ViewBag.OrderStatusData = orderStatusData;
            ViewBag.CategoryData = categoryData;
            ViewBag.OutgoingReasons = outgoingReasons;
            ViewBag.MonthlyOrders = monthlyOrders;

            return View();
        }

        // GET: Report/Inventory
        public async Task<IActionResult> Inventory(DateTime? startDate, DateTime? endDate, string format = "pdf")
        {
            var products = await _context.Products
                .OrderBy(p => p.Name)
                .ToListAsync();

            if (format.ToLower() == "excel")
            {
                return await ExportInventoryToExcel(products);
            }
            else
            {
                return await ExportInventoryToPdf(products);
            }
        }

        // GET: Report/Supplier
        public async Task<IActionResult> Supplier(string format = "pdf")
        {
            var suppliers = await _context.Suppliers
                .OrderBy(s => s.Name)
                .ToListAsync();

            if (format.ToLower() == "excel")
            {
                return await ExportSupplierToExcel(suppliers);
            }
            else
            {
                return await ExportSupplierToPdf(suppliers);
            }
        }

        // GET: Report/Outgoing
        public async Task<IActionResult> Outgoing(DateTime? startDate, DateTime? endDate, string format = "pdf")
        {
            var outgoings = _context.Outgoings.Include(o => o.Product).AsQueryable();

            if (startDate.HasValue)
            {
                outgoings = outgoings.Where(o => o.OutgoingDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                outgoings = outgoings.Where(o => o.OutgoingDate <= endDate.Value);
            }

            var outgoingList = await outgoings.OrderByDescending(o => o.OutgoingDate).ToListAsync();

            if (format.ToLower() == "excel")
            {
                return await ExportOutgoingToExcel(outgoingList);
            }
            else
            {
                return await ExportOutgoingToPdf(outgoingList);
            }
        }

        // GET: Report/Order
        public async Task<IActionResult> Order(DateTime? startDate, DateTime? endDate, string format = "pdf")
        {
            var orders = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (startDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate <= endDate.Value);
            }

            var orderList = await orders.OrderByDescending(o => o.OrderDate).ToListAsync();

            if (format.ToLower() == "excel")
            {
                return await ExportOrderToExcel(orderList);
            }
            else
            {
                return await ExportOrderToPdf(orderList);
            }
        }

        // GET: Report/Employee
        public async Task<IActionResult> Employee(string format = "pdf")
        {
            var employees = await _context.Employees
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();

            if (format.ToLower() == "excel")
            {
                return await ExportEmployeeToExcel(employees);
            }
            else
            {
                return await ExportEmployeeToPdf(employees);
            }
        }

        // GET: Report/Salary
        public async Task<IActionResult> Salary(int? month, int? year, string format = "pdf")
        {
            var salaries = _context.Salaries.Include(s => s.Employee).AsQueryable();

            if (month.HasValue)
            {
                salaries = salaries.Where(s => s.Month == month.Value);
            }

            if (year.HasValue)
            {
                salaries = salaries.Where(s => s.Year == year.Value);
            }

            var salaryList = await salaries.OrderByDescending(s => s.Year)
                .ThenByDescending(s => s.Month)
                .ToListAsync();

            if (format.ToLower() == "excel")
            {
                return await ExportSalaryToExcel(salaryList);
            }
            else
            {
                return await ExportSalaryToPdf(salaryList);
            }
        }

        // Excel Export Methods
        private async Task<FileResult> ExportInventoryToExcel(List<Models.Product> products)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Inventory");

            worksheet.Cells[1, 1].Value = "Product Name";
            worksheet.Cells[1, 2].Value = "SKU";
            worksheet.Cells[1, 3].Value = "Category";
            worksheet.Cells[1, 4].Value = "Quantity";
            worksheet.Cells[1, 5].Value = "Cost Price";
            worksheet.Cells[1, 6].Value = "Selling Price";
            worksheet.Cells[1, 7].Value = "Supplier";

            for (int i = 0; i < products.Count; i++)
            {
                var product = products[i];
                worksheet.Cells[i + 2, 1].Value = product.Name;
                worksheet.Cells[i + 2, 2].Value = product.SKU;
                worksheet.Cells[i + 2, 3].Value = product.Category;
                worksheet.Cells[i + 2, 4].Value = product.QuantityInStock;
                worksheet.Cells[i + 2, 5].Value = product.CostPrice;
                worksheet.Cells[i + 2, 6].Value = product.SellingPrice;
                worksheet.Cells[i + 2, 7].Value = product.Supplier;
            }

            worksheet.Cells.AutoFitColumns();
            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Inventory_Report.xlsx");
        }

        private async Task<FileResult> ExportSupplierToExcel(List<Models.Supplier> suppliers)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Suppliers");

            worksheet.Cells[1, 1].Value = "Supplier Name";
            worksheet.Cells[1, 2].Value = "Category";
            worksheet.Cells[1, 3].Value = "Contact Person";
            worksheet.Cells[1, 4].Value = "Phone Number";
            worksheet.Cells[1, 5].Value = "Email";
            worksheet.Cells[1, 6].Value = "Physical Address";
            worksheet.Cells[1, 7].Value = "Status";
            worksheet.Cells[1, 8].Value = "Created Date";

            for (int i = 0; i < suppliers.Count; i++)
            {
                var supplier = suppliers[i];
                worksheet.Cells[i + 2, 1].Value = supplier.Name;
                worksheet.Cells[i + 2, 2].Value = supplier.Category;
                worksheet.Cells[i + 2, 3].Value = supplier.ContactPersonName;
                worksheet.Cells[i + 2, 4].Value = supplier.PhoneNumber;
                worksheet.Cells[i + 2, 5].Value = supplier.EmailAddress;
                worksheet.Cells[i + 2, 6].Value = supplier.PhysicalAddress;
                worksheet.Cells[i + 2, 7].Value = supplier.IsActive ? "Active" : "Inactive";
                worksheet.Cells[i + 2, 8].Value = supplier.CreatedDate.ToString("yyyy-MM-dd");
            }

            worksheet.Cells.AutoFitColumns();
            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Supplier_Report.xlsx");
        }

        private async Task<FileResult> ExportOutgoingToExcel(List<Models.Outgoing> outgoings)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Outgoings");

            worksheet.Cells[1, 1].Value = "Date";
            worksheet.Cells[1, 2].Value = "Product";
            worksheet.Cells[1, 3].Value = "Quantity";
            worksheet.Cells[1, 4].Value = "Reason";
            worksheet.Cells[1, 5].Value = "Recipient";
            worksheet.Cells[1, 6].Value = "Total Amount";

            for (int i = 0; i < outgoings.Count; i++)
            {
                var outgoing = outgoings[i];
                worksheet.Cells[i + 2, 1].Value = outgoing.OutgoingDate.ToString("yyyy-MM-dd");
                worksheet.Cells[i + 2, 2].Value = outgoing.Product?.Name;
                worksheet.Cells[i + 2, 3].Value = outgoing.Quantity;
                worksheet.Cells[i + 2, 4].Value = outgoing.Reason;
                worksheet.Cells[i + 2, 5].Value = outgoing.Recipient;
                worksheet.Cells[i + 2, 6].Value = outgoing.TotalAmount;
            }

            worksheet.Cells.AutoFitColumns();
            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Outgoing_Report.xlsx");
        }

        private async Task<FileResult> ExportOrderToExcel(List<Models.Order> orders)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Orders");

            worksheet.Cells[1, 1].Value = "Order Number";
            worksheet.Cells[1, 2].Value = "Date";
            worksheet.Cells[1, 3].Value = "Customer";
            worksheet.Cells[1, 4].Value = "Status";
            worksheet.Cells[1, 5].Value = "Total Amount";

            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                worksheet.Cells[i + 2, 1].Value = order.OrderNumber;
                worksheet.Cells[i + 2, 2].Value = order.OrderDate.ToString("yyyy-MM-dd");
                worksheet.Cells[i + 2, 3].Value = order.CustomerName;
                worksheet.Cells[i + 2, 4].Value = order.Status;
                worksheet.Cells[i + 2, 5].Value = order.TotalAmount;
            }

            worksheet.Cells.AutoFitColumns();
            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Order_Report.xlsx");
        }

        private async Task<FileResult> ExportEmployeeToExcel(List<Models.Employee> employees)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Employees");

            worksheet.Cells[1, 1].Value = "Name";
            worksheet.Cells[1, 2].Value = "Email";
            worksheet.Cells[1, 3].Value = "Phone";
            worksheet.Cells[1, 4].Value = "Position";
            worksheet.Cells[1, 5].Value = "Department";
            worksheet.Cells[1, 6].Value = "Hire Date";
            worksheet.Cells[1, 7].Value = "Salary";
            worksheet.Cells[1, 8].Value = "Status";

            for (int i = 0; i < employees.Count; i++)
            {
                var employee = employees[i];
                worksheet.Cells[i + 2, 1].Value = employee.FullName;
                worksheet.Cells[i + 2, 2].Value = employee.Email;
                worksheet.Cells[i + 2, 3].Value = employee.Phone;
                worksheet.Cells[i + 2, 4].Value = employee.Position;
                worksheet.Cells[i + 2, 5].Value = employee.Department;
                worksheet.Cells[i + 2, 6].Value = employee.HireDate?.ToString("yyyy-MM-dd");
                worksheet.Cells[i + 2, 7].Value = employee.Salary;
                worksheet.Cells[i + 2, 8].Value = employee.IsActive ? "Active" : "Inactive";
            }

            worksheet.Cells.AutoFitColumns();
            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Employee_Report.xlsx");
        }

        private async Task<FileResult> ExportSalaryToExcel(List<Models.Salary> salaries)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Salaries");

            worksheet.Cells[1, 1].Value = "Employee";
            worksheet.Cells[1, 2].Value = "Month";
            worksheet.Cells[1, 3].Value = "Year";
            worksheet.Cells[1, 4].Value = "Salary Type";
            worksheet.Cells[1, 5].Value = "Salary Amount";
            worksheet.Cells[1, 6].Value = "Payment Date";
            worksheet.Cells[1, 7].Value = "Status";

            for (int i = 0; i < salaries.Count; i++)
            {
                var salary = salaries[i];
                worksheet.Cells[i + 2, 1].Value = salary.Employee?.FullName;
                worksheet.Cells[i + 2, 2].Value = salary.Month;
                worksheet.Cells[i + 2, 3].Value = salary.Year;
                worksheet.Cells[i + 2, 4].Value = salary.SalaryType;
                worksheet.Cells[i + 2, 5].Value = salary.SalaryAmount;
                worksheet.Cells[i + 2, 6].Value = salary.PaymentDate?.ToString("yyyy-MM-dd");
                worksheet.Cells[i + 2, 7].Value = salary.IsPaid ? "Paid" : "Unpaid";
            }

            worksheet.Cells.AutoFitColumns();
            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Salary_Report.xlsx");
        }

        // PDF Export Methods (simplified - using QuestPDF)
        private async Task<FileResult> ExportInventoryToPdf(List<Models.Product> products)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Beautiful Header
                    page.Header().Column(column =>
                    {
                        column.Item().Background(Colors.Blue.Medium).Padding(15).Column(headerColumn =>
                        {
                            headerColumn.Item().Text("INVENTORY REPORT").Bold().FontSize(20).FontColor(Colors.White);
                            headerColumn.Item().Text($"Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm}").FontSize(10).FontColor(Colors.Grey.Lighten2);
                        });
                    });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // Summary Section
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                        {
                            row.ConstantItem(150).Text($"Total Products: {products.Count}").Bold().FontSize(11);
                            row.RelativeItem().AlignRight().Text($"Total Value: ${products.Sum(p => p.QuantityInStock * p.SellingPrice):N2}").Bold().FontSize(11);
                        });

                        column.Item().PaddingTop(10);

                        // Beautiful Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            // Header with background color
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken1).Padding(8).Text("Product Name").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Blue.Darken1).Padding(8).Text("SKU").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Blue.Darken1).Padding(8).Text("Category").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Blue.Darken1).Padding(8).Text("Quantity").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Blue.Darken1).Padding(8).Text("Cost Price").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Blue.Darken1).Padding(8).Text("Selling Price").Bold().FontColor(Colors.White).FontSize(10);
                            });

                            // Alternating row colors
                            int rowIndex = 0;
                            foreach (var product in products)
                            {
                                var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                table.Cell().Background(bgColor).Padding(6).Text(product.Name ?? "N/A").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(product.SKU ?? "N/A").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(product.Category ?? "N/A").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(product.QuantityInStock.ToString()).FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text($"${product.CostPrice:N2}").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text($"${product.SellingPrice:N2}").FontSize(9);
                                rowIndex++;
                            }
                        });
                    });

                    // Footer
                    page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:yyyy-MM-dd}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/pdf", "Inventory_Report.pdf");
        }

        private async Task<FileResult> ExportSupplierToPdf(List<Models.Supplier> suppliers)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Beautiful Header with gradient background
                    page.Header().Column(column =>
                    {
                        column.Item().Background(Colors.Purple.Medium).Padding(15).Column(headerColumn =>
                        {
                            headerColumn.Item().Text("SUPPLIER REPORT").Bold().FontSize(20).FontColor(Colors.White);
                            headerColumn.Item().Text($"Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm}").FontSize(10).FontColor(Colors.Grey.Lighten2);
                        });
                    });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // Summary Section
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                        {
                            row.ConstantItem(150).Text($"Total Suppliers: {suppliers.Count}").Bold().FontSize(11);
                            row.RelativeItem().AlignRight().Text($"Active: {suppliers.Count(s => s.IsActive)} | Inactive: {suppliers.Count(s => !s.IsActive)}").Bold().FontSize(11);
                        });

                        column.Item().PaddingTop(10);

                        // Beautiful Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                            });

                            // Header with background color
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Purple.Darken1).Padding(8).Text("Supplier Name").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Purple.Darken1).Padding(8).Text("Category").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Purple.Darken1).Padding(8).Text("Contact Person").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Purple.Darken1).Padding(8).Text("Contact Info").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Purple.Darken1).Padding(8).Text("Address").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Purple.Darken1).Padding(8).Text("Status").Bold().FontColor(Colors.White).FontSize(10);
                            });

                            // Alternating row colors
                            int rowIndex = 0;
                            foreach (var supplier in suppliers)
                            {
                                var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                table.Cell().Background(bgColor).Padding(6).Text(supplier.Name ?? "N/A").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(supplier.Category ?? "N/A").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(supplier.ContactPersonName ?? "N/A").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text($"{supplier.PhoneNumber ?? "N/A"}\n{supplier.EmailAddress ?? ""}").FontSize(8);
                                table.Cell().Background(bgColor).Padding(6).Text(supplier.PhysicalAddress ?? "N/A").FontSize(8);
                                table.Cell().Background(bgColor).Padding(6).Text(supplier.IsActive ? "Active" : "Inactive").FontSize(9).FontColor(supplier.IsActive ? Colors.Green.Darken1 : Colors.Red.Darken1);
                                rowIndex++;
                            }
                        });
                    });

                    // Footer
                    page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:yyyy-MM-dd}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/pdf", "Supplier_Report.pdf");
        }

        private async Task<FileResult> ExportOutgoingToPdf(List<Models.Outgoing> outgoings)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Beautiful Header
                    page.Header().Column(column =>
                    {
                        column.Item().Background(Colors.Red.Medium).Padding(15).Column(headerColumn =>
                        {
                            headerColumn.Item().Text("OUTGOING REPORT").Bold().FontSize(20).FontColor(Colors.White);
                            headerColumn.Item().Text($"Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm}").FontSize(10).FontColor(Colors.Grey.Lighten2);
                        });
                    });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // Summary Section
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                        {
                            row.ConstantItem(150).Text($"Total Outgoings: {outgoings.Count}").Bold().FontSize(11);
                            row.RelativeItem().AlignRight().Text($"Total Quantity: {outgoings.Sum(o => o.Quantity)}").Bold().FontSize(11);
                        });

                        column.Item().PaddingTop(10);

                        // Beautiful Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Red.Darken1).Padding(8).Text("Date").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Red.Darken1).Padding(8).Text("Product").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Red.Darken1).Padding(8).Text("Quantity").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Red.Darken1).Padding(8).Text("Reason").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Red.Darken1).Padding(8).Text("Recipient").Bold().FontColor(Colors.White).FontSize(10);
                            });

                            int rowIndex = 0;
                            foreach (var outgoing in outgoings)
                            {
                                var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                table.Cell().Background(bgColor).Padding(6).Text(outgoing.OutgoingDate.ToString("yyyy-MM-dd")).FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(outgoing.Product?.Name ?? "N/A").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(outgoing.Quantity.ToString()).FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(outgoing.Reason ?? "N/A").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(outgoing.Recipient ?? "N/A").FontSize(9);
                                rowIndex++;
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:yyyy-MM-dd}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/pdf", "Outgoing_Report.pdf");
        }

        private async Task<FileResult> ExportOrderToPdf(List<Models.Order> orders)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Beautiful Header
                    page.Header().Column(column =>
                    {
                        column.Item().Background(Colors.Green.Medium).Padding(15).Column(headerColumn =>
                        {
                            headerColumn.Item().Text("ORDER REPORT").Bold().FontSize(20).FontColor(Colors.White);
                            headerColumn.Item().Text($"Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm}").FontSize(10).FontColor(Colors.Grey.Lighten2);
                        });
                    });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // Summary Section
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                        {
                            row.ConstantItem(150).Text($"Total Orders: {orders.Count}").Bold().FontSize(11);
                            row.RelativeItem().AlignRight().Text($"Total Value: ${orders.Sum(o => o.TotalAmount):N2}").Bold().FontSize(11);
                        });

                        column.Item().PaddingTop(10);

                        // Beautiful Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Green.Darken1).Padding(8).Text("Order #").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Green.Darken1).Padding(8).Text("Date").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Green.Darken1).Padding(8).Text("Customer").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Green.Darken1).Padding(8).Text("Status").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Green.Darken1).Padding(8).Text("Total").Bold().FontColor(Colors.White).FontSize(10);
                            });

                            int rowIndex = 0;
                            foreach (var order in orders)
                            {
                                var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                var statusColor = order.Status == "Delivered" ? Colors.Green.Darken1 : 
                                                 order.Status == "Pending" ? Colors.Orange.Darken1 : 
                                                 order.Status == "Cancelled" ? Colors.Red.Darken1 : Colors.Blue.Darken1;
                                
                                table.Cell().Background(bgColor).Padding(6).Text(order.OrderNumber).FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(order.OrderDate.ToString("yyyy-MM-dd")).FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(order.CustomerName ?? "N/A").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(order.Status).FontSize(9).FontColor(statusColor);
                                table.Cell().Background(bgColor).Padding(6).Text($"${order.TotalAmount:N2}").FontSize(9).Bold();
                                rowIndex++;
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:yyyy-MM-dd}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/pdf", "Order_Report.pdf");
        }

        private async Task<FileResult> ExportEmployeeToPdf(List<Models.Employee> employees)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Beautiful Header
                    page.Header().Column(column =>
                    {
                        column.Item().Background(Colors.Teal.Medium).Padding(15).Column(headerColumn =>
                        {
                            headerColumn.Item().Text("EMPLOYEE REPORT").Bold().FontSize(20).FontColor(Colors.White);
                            headerColumn.Item().Text($"Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm}").FontSize(10).FontColor(Colors.Grey.Lighten2);
                        });
                    });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // Summary Section
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                        {
                            row.ConstantItem(150).Text($"Total Employees: {employees.Count}").Bold().FontSize(11);
                            row.RelativeItem().AlignRight().Text($"Active: {employees.Count(e => e.IsActive)} | Inactive: {employees.Count(e => !e.IsActive)}").Bold().FontSize(11);
                        });

                        column.Item().PaddingTop(10);

                        // Beautiful Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Teal.Darken1).Padding(8).Text("Name").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Teal.Darken1).Padding(8).Text("Email").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Teal.Darken1).Padding(8).Text("Position").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Teal.Darken1).Padding(8).Text("Department").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Teal.Darken1).Padding(8).Text("Status").Bold().FontColor(Colors.White).FontSize(10);
                            });

                            int rowIndex = 0;
                            foreach (var employee in employees)
                            {
                                var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                table.Cell().Background(bgColor).Padding(6).Text(employee.FullName).FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(employee.Email).FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(employee.Position ?? "N/A").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(employee.Department ?? "N/A").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(employee.IsActive ? "Active" : "Inactive").FontSize(9).FontColor(employee.IsActive ? Colors.Green.Darken1 : Colors.Red.Darken1);
                                rowIndex++;
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:yyyy-MM-dd}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/pdf", "Employee_Report.pdf");
        }

        private async Task<FileResult> ExportSalaryToPdf(List<Models.Salary> salaries)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Beautiful Header
                    page.Header().Column(column =>
                    {
                        column.Item().Background(Colors.Orange.Medium).Padding(15).Column(headerColumn =>
                        {
                            headerColumn.Item().Text("SALARY REPORT").Bold().FontSize(20).FontColor(Colors.White);
                            headerColumn.Item().Text($"Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm}").FontSize(10).FontColor(Colors.Grey.Lighten2);
                        });
                    });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // Summary Section
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                        {
                            row.ConstantItem(150).Text($"Total Records: {salaries.Count}").Bold().FontSize(11);
                            row.RelativeItem().AlignRight().Text($"Total Paid: ${salaries.Where(s => s.IsPaid).Sum(s => s.SalaryAmount):N2} | Unpaid: ${salaries.Where(s => !s.IsPaid).Sum(s => s.SalaryAmount):N2}").Bold().FontSize(11);
                        });

                        column.Item().PaddingTop(10);

                        // Beautiful Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Orange.Darken1).Padding(8).Text("Employee").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Orange.Darken1).Padding(8).Text("Month/Year").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Orange.Darken1).Padding(8).Text("Type").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Orange.Darken1).Padding(8).Text("Amount").Bold().FontColor(Colors.White).FontSize(10);
                                header.Cell().Background(Colors.Orange.Darken1).Padding(8).Text("Status").Bold().FontColor(Colors.White).FontSize(10);
                            });

                            int rowIndex = 0;
                            foreach (var salary in salaries)
                            {
                                var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                table.Cell().Background(bgColor).Padding(6).Text(salary.Employee?.FullName ?? "N/A").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text($"{salary.Month:00}/{salary.Year}").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text(salary.SalaryType ?? "N/A").FontSize(9);
                                table.Cell().Background(bgColor).Padding(6).Text($"${salary.SalaryAmount:N2}").FontSize(9).Bold();
                                table.Cell().Background(bgColor).Padding(6).Text(salary.IsPaid ? "Paid" : "Unpaid").FontSize(9).FontColor(salary.IsPaid ? Colors.Green.Darken1 : Colors.Red.Darken1);
                                rowIndex++;
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:yyyy-MM-dd}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/pdf", "Salary_Report.pdf");
        }

        // POST: Report/DeleteInventory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInventory()
        {
            var products = await _context.Products.ToListAsync();
            _context.Products.RemoveRange(products);
            await _context.SaveChangesAsync();
            TempData["Success"] = "All inventory products deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Report/DeleteSuppliers
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSuppliers()
        {
            var suppliers = await _context.Suppliers.ToListAsync();
            _context.Suppliers.RemoveRange(suppliers);
            await _context.SaveChangesAsync();
            TempData["Success"] = "All suppliers deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Report/DeleteOutgoings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOutgoings()
        {
            var outgoings = await _context.Outgoings.ToListAsync();
            _context.Outgoings.RemoveRange(outgoings);
            await _context.SaveChangesAsync();
            TempData["Success"] = "All outgoing records deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Report/DeleteOrders
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrders()
        {
            var orders = await _context.Orders.Include(o => o.OrderItems).ToListAsync();
            foreach (var order in orders)
            {
                _context.OrderItems.RemoveRange(order.OrderItems);
            }
            _context.Orders.RemoveRange(orders);
            await _context.SaveChangesAsync();
            TempData["Success"] = "All orders deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Report/DeleteEmployees
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployees()
        {
            var employees = await _context.Employees.ToListAsync();
            _context.Employees.RemoveRange(employees);
            await _context.SaveChangesAsync();
            TempData["Success"] = "All employees deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Report/DeleteSalaries
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSalaries()
        {
            var salaries = await _context.Salaries.ToListAsync();
            _context.Salaries.RemoveRange(salaries);
            await _context.SaveChangesAsync();
            TempData["Success"] = "All salary records deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}

