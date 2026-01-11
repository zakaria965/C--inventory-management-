using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Controllers
{
    public class OutgoingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OutgoingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Outgoing/Create
        public IActionResult Create()
        {
            // By default, show products with stock > 0 (for outgoing)
            // When user selects "Return", JavaScript will update the product list to include all products
            ViewData["ProductId"] = new SelectList(_context.Products.Where(p => p.QuantityInStock > 0).OrderBy(p => p.Name), "Id", "Name");
            ViewData["OrderId"] = new SelectList(_context.Orders.OrderByDescending(o => o.OrderDate), "Id", "OrderNumber");
            return View();
        }

        // POST: Outgoing/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Outgoing outgoing)
        {
            var product = await _context.Products.FindAsync(outgoing.ProductId);
            
            if (product == null)
            {
                ModelState.AddModelError("ProductId", "Product not found");
            }
            else
            {
                // For "Return" reason, we ADD stock back (no need to check availability)
                // For other reasons, we SUBTRACT stock (need to check availability)
                bool isReturn = outgoing.Reason?.Equals("Return", StringComparison.OrdinalIgnoreCase) ?? false;
                
                if (!isReturn && product.QuantityInStock < outgoing.Quantity)
                {
                    ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.QuantityInStock}");
                }
            }

            if (ModelState.IsValid)
            {
                outgoing.OutgoingDate = DateTime.Now;
                if (outgoing.OutgoingPrice.HasValue)
                {
                    outgoing.TotalAmount = outgoing.Quantity * outgoing.OutgoingPrice.Value;
                }

                _context.Outgoings.Add(outgoing);

                // Update product stock based on reason
                if (product != null)
                {
                    bool isReturn = outgoing.Reason?.Equals("Return", StringComparison.OrdinalIgnoreCase) ?? false;
                    
                    if (isReturn)
                    {
                        // Return: ADD quantity back to inventory
                        product.QuantityInStock += outgoing.Quantity;
                        TempData["Success"] = $"Return recorded successfully! {outgoing.Quantity} units added back to inventory.";
                    }
                    else
                    {
                        // Other reasons: SUBTRACT quantity from inventory
                        product.QuantityInStock -= outgoing.Quantity;
                        TempData["Success"] = "Outgoing recorded successfully!";
                    }
                    
                    product.LastUpdated = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(History));
            }
            
            // For Return, allow products with 0 stock; for others, only products with stock > 0
            bool isReturnReason = outgoing.Reason?.Equals("Return", StringComparison.OrdinalIgnoreCase) ?? false;
            var productsQuery = isReturnReason 
                ? _context.Products.OrderBy(p => p.Name) 
                : _context.Products.Where(p => p.QuantityInStock > 0).OrderBy(p => p.Name);
            
            ViewData["ProductId"] = new SelectList(productsQuery, "Id", "Name", outgoing.ProductId);
            ViewData["OrderId"] = new SelectList(_context.Orders.OrderByDescending(o => o.OrderDate), "Id", "OrderNumber", outgoing.OrderId);
            return View(outgoing);
        }

        // GET: Outgoing/History
        public async Task<IActionResult> History(string searchString, DateTime? startDate, DateTime? endDate)
        {
            var outgoings = from o in _context.Outgoings.Include(o => o.Product) select o;

            if (!string.IsNullOrEmpty(searchString))
            {
                outgoings = outgoings.Where(o => o.Product != null && 
                    (o.Product.Name.Contains(searchString) || o.Product.SKU.Contains(searchString) || 
                     (o.Recipient != null && o.Recipient.Contains(searchString))));
            }

            if (startDate.HasValue)
            {
                outgoings = outgoings.Where(o => o.OutgoingDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                outgoings = outgoings.Where(o => o.OutgoingDate <= endDate.Value);
            }

            ViewBag.SearchString = searchString;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(await outgoings.OrderByDescending(o => o.OutgoingDate).ToListAsync());
        }

        // GET: Outgoing/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var outgoing = await _context.Outgoings
                .Include(o => o.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (outgoing == null)
            {
                return NotFound();
            }

            return View(outgoing);
        }

        // GET: Outgoing/GetAllProducts
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _context.Products
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    stock = p.QuantityInStock
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                products = products
            });
        }

        // GET: Outgoing/GetOrderDetails/5
        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return Json(new { success = false, message = "Order not found" });
            }

            if (order.OrderItems == null || !order.OrderItems.Any())
            {
                return Json(new { 
                    success = false, 
                    message = "Order has no items",
                    customerName = order.CustomerName ?? "",
                    shippingAddress = order.ShippingAddress ?? "",
                    notes = order.Notes ?? ""
                });
            }

            // Return all order details including all items
            return Json(new
            {
                success = true,
                orderNumber = order.OrderNumber,
                customerName = order.CustomerName ?? "",
                customerEmail = order.CustomerEmail ?? "",
                customerPhone = order.CustomerPhone ?? "",
                shippingAddress = order.ShippingAddress ?? "",
                orderDate = order.OrderDate.ToString("yyyy-MM-dd"),
                status = order.Status,
                totalAmount = order.TotalAmount,
                notes = order.Notes ?? "",
                items = order.OrderItems.Select(oi => new
                {
                    productId = oi.ProductId,
                    productName = oi.Product?.Name ?? "",
                    productSku = oi.Product?.SKU ?? "",
                    quantity = oi.Quantity,
                    unitPrice = oi.UnitPrice,
                    totalPrice = oi.TotalPrice
                }).ToList()
            });
        }
    }
}
