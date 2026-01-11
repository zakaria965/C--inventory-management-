using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InventoryManagementSystem.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Order
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            var orders = from o in _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product) select o;

            if (!string.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(o => o.OrderNumber.Contains(searchString) 
                    || (o.CustomerName != null && o.CustomerName.Contains(searchString))
                    || (o.CustomerEmail != null && o.CustomerEmail.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                orders = orders.Where(o => o.Status == statusFilter);
            }

            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.Statuses = new List<string> { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

            return View(await orders.OrderByDescending(o => o.OrderDate).ToListAsync());
        }

        // GET: Order/AdminOrders
        public async Task<IActionResult> AdminOrders()
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "User";
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Index));
            }

            var pendingOrders = await _context.Orders
                .Where(o => o.Status == "Pending")
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(pendingOrders);
        }

        // GET: Order/Create
        public async Task<IActionResult> Create()
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "User";
            if (role.ToLower() != "user")
            {
                // Admins should not create orders via this page
                return RedirectToAction(nameof(Index));
            }

            var products = await _context.Products
                .Where(p => p.QuantityInStock > 0)
                .OrderBy(p => p.Name)
                .ToListAsync();

            ViewData["Products"] = new SelectList(products, "Id", "Name");
            return View();
        }

        // GET: Order/GetProductPrice/5
        [HttpGet]
        public async Task<IActionResult> GetProductPrice(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                return Json(new { price = product.SellingPrice, stock = product.QuantityInStock });
            }
            return Json(new { price = 0, stock = 0 });
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order, List<int> productIds, List<int> quantities, List<decimal> unitPrices)
        {
            var roleCheck = HttpContext.Session.GetString("UserRole") ?? "User";
            if (roleCheck.ToLower() != "user")
            {
                // Admins are not allowed to post order creation here
                return RedirectToAction(nameof(Index));
            }
            // Remove OrderNumber from validation - it's auto-generated
            ModelState.Remove(nameof(order.OrderNumber));
            
            // Allow empty email and shipping address
            if (string.IsNullOrWhiteSpace(order.CustomerEmail))
            {
                ModelState.Remove(nameof(order.CustomerEmail));
                order.CustomerEmail = null;
            }
            if (string.IsNullOrWhiteSpace(order.ShippingAddress))
            {
                ModelState.Remove(nameof(order.ShippingAddress));
                order.ShippingAddress = null;
            }
            if (string.IsNullOrWhiteSpace(order.CustomerName))
            {
                ModelState.Remove(nameof(order.CustomerName));
                order.CustomerName = null;
            }
            if (string.IsNullOrWhiteSpace(order.CustomerPhone))
            {
                ModelState.Remove(nameof(order.CustomerPhone));
                order.CustomerPhone = null;
            }

            if (productIds == null || productIds.Count == 0)
            {
                ModelState.AddModelError("", "Please add at least one product to the order.");
            }

            if (ModelState.IsValid && productIds != null && productIds.Count > 0)
            {
                // Generate order number
                order.OrderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                order.OrderDate = DateTime.Now;
                order.Status = "Pending";
                order.TotalAmount = 0;

                // Assign order to logged in user if not provided
                var userEmail = HttpContext.Session.GetString("UserEmail");
                var userName = HttpContext.Session.GetString("UserName");
                if (string.IsNullOrWhiteSpace(order.CustomerEmail) && !string.IsNullOrWhiteSpace(userEmail)) order.CustomerEmail = userEmail;
                if (string.IsNullOrWhiteSpace(order.CustomerName) && !string.IsNullOrWhiteSpace(userName)) order.CustomerName = userName;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Add order items
                for (int i = 0; i < productIds.Count; i++)
                {
                    var product = await _context.Products.FindAsync(productIds[i]);
                    if (product != null)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.Id,
                            ProductId = productIds[i],
                            Quantity = quantities[i],
                            UnitPrice = unitPrices[i],
                            TotalPrice = quantities[i] * unitPrices[i]
                        };
                        order.TotalAmount += orderItem.TotalPrice;

                        _context.OrderItems.Add(orderItem);

                        // Only decrease stock immediately if created by admin (admin-created orders are processed immediately)
                        if (roleCheck.ToLower() != "user")
                        {
                            if (product.QuantityInStock >= quantities[i])
                            {
                                product.QuantityInStock -= quantities[i];
                                product.LastUpdated = DateTime.Now;
                            }
                            else
                            {
                                // Not enough stock for admin immediate purchase - mark item quantity to available max
                                // (Alternatively, admin should be prevented earlier)
                                orderItem.Quantity = Math.Min(orderItem.Quantity, product.QuantityInStock);
                                product.LastUpdated = DateTime.Now;
                            }
                        }
                    }
                }

                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Order created successfully!";

                if (roleCheck.ToLower() == "user") return RedirectToAction("UserDashboard", "Dashboard");
                return RedirectToAction(nameof(Index));
            }

            // If validation fails, reload products
            var products = await _context.Products
                .Where(p => p.QuantityInStock > 0)
                .OrderBy(p => p.Name)
                .ToListAsync();
            ViewData["Products"] = new SelectList(products, "Id", "Name");
            
            // Show validation errors
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Error"] = string.Join(", ", errors);
            }
            
            return View(order);
        }

        // GET: Order/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Order/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                var oldStatus = order.Status;
                // When admin moves order to Processing (accepts), allocate stock now
                if (status == "Processing" && oldStatus != "Processing")
                {
                    // Check stock availability
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null && item.Product.QuantityInStock < item.Quantity)
                        {
                            TempData["Error"] = $"Not enough stock for product {item.Product.Name}. Cannot accept order.";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    // Deduct stock
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null)
                        {
                            item.Product.QuantityInStock -= item.Quantity;
                            item.Product.LastUpdated = DateTime.Now;
                        }
                    }
                }

                // If cancelling from a processed state, rollback stock
                if (status == "Cancelled" && oldStatus == "Processing")
                {
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null)
                        {
                            item.Product.QuantityInStock += item.Quantity;
                            item.Product.LastUpdated = DateTime.Now;
                        }
                    }
                }

                order.Status = status;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Order status updated successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Order/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null && order.Status != "Cancelled")
            {
                // Only rollback stock if it was previously allocated (Processing)
                if (order.Status == "Processing")
                {
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null)
                        {
                            item.Product.QuantityInStock += item.Quantity;
                            item.Product.LastUpdated = DateTime.Now;
                        }
                    }
                }

                order.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Order cancelled successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Order/Accept/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "User";
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(AdminOrders));
            }

            if (order.Status != "Pending")
            {
                TempData["Error"] = "Only pending orders can be accepted.";
                return RedirectToAction(nameof(AdminOrders));
            }

            // Verify stock availability
            foreach (var item in order.OrderItems)
            {
                if (item.Product == null || item.Product.QuantityInStock < item.Quantity)
                {
                    TempData["Error"] = $"Not enough stock for product {item.Product?.Name ?? "(unknown)"}. Cannot accept order.";
                    return RedirectToAction(nameof(AdminOrders));
                }
            }

            // Deduct stock
            foreach (var item in order.OrderItems)
            {
                if (item.Product != null)
                {
                    item.Product.QuantityInStock -= item.Quantity;
                    item.Product.LastUpdated = DateTime.Now;
                }
            }

            // Mark order as paid/approved and record payment date
            order.Status = "Paid";
            order.PaymentDate = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Order accepted, payment recorded, and stock updated.";
            return RedirectToAction(nameof(AdminOrders));
        }

        // POST: Order/Deny/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deny(int id)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "User";
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(AdminOrders));
            }

            if (order.Status != "Pending")
            {
                TempData["Error"] = "Only pending orders can be denied.";
                return RedirectToAction(nameof(AdminOrders));
            }

            order.Status = "Denied";
            // Do not deduct stock and keep payment as unpaid (PaymentDate remains null)

            await _context.SaveChangesAsync();
            TempData["Success"] = "Order denied.";
            return RedirectToAction(nameof(AdminOrders));
        }

        // GET: Order/Export
        public async Task<IActionResult> Export(string format = "excel")
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            if (format.ToLower() == "excel")
            {
                using var package = new OfficeOpenXml.ExcelPackage();
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

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Orders.xlsx");
            }
            else
            {
                // PDF export
                var document = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4);
                        page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Text("Orders Report").Bold().FontSize(16);
                        page.Content().Column(column =>
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Order #").Bold();
                                    header.Cell().Text("Date").Bold();
                                    header.Cell().Text("Customer").Bold();
                                    header.Cell().Text("Status").Bold();
                                    header.Cell().Text("Total").Bold();
                                });

                                foreach (var order in orders)
                                {
                                    table.Cell().Text(order.OrderNumber);
                                    table.Cell().Text(order.OrderDate.ToString("yyyy-MM-dd"));
                                    table.Cell().Text(order.CustomerName ?? "N/A");
                                    table.Cell().Text(order.Status);
                                    table.Cell().Text($"${order.TotalAmount:N2}");
                                }
                            });
                        });
                    });
                });

                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                return File(stream.ToArray(), "application/pdf", "Orders.pdf");
            }
        }
    }
}
