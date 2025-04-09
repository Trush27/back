I'll help you create a comprehensive solution for displaying order details when a user clicks on the eye icon in the orders page. Here's the implementation:

### 1. First, create a ViewModel for Order Details

```csharp
// OrderDetailsViewModel.cs
namespace Entity.ViewModel
{
    public class OrderDetailsViewModel
    {
        public int OrderId { get; set; }
        public string InvoiceNumber { get; set; }
        public string Status { get; set; }
        public string PaymentMode { get; set; }
        public DateTime PaidOn { get; set; }
        public DateTime PlacedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public TimeSpan OrderDuration { get; set; }
        public double TotalAmount { get; set; }
        public double SubTotal { get; set; }
        
        // Customer Details
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public int NumberOfPersons { get; set; }
        
        // Table Details
        public string TableName { get; set; }
        public string SectionName { get; set; }
        
        // Order Items
        public List<OrderItemViewModel> OrderItems { get; set; }
        
        // Taxes
        public List<OrderTaxViewModel> Taxes { get; set; }
    }

    public class OrderItemViewModel
    {
        public int SrNo { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public double TotalAmount { get; set; }
        public List<OrderItemModifierViewModel> Modifiers { get; set; }
    }

    public class OrderItemModifierViewModel
    {
        public string ModifierName { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public double TotalAmount { get; set; }
    }

    public class OrderTaxViewModel
    {
        public string TaxName { get; set; }
        public double TaxValue { get; set; }
    }
}
```

### 2. Update the OrderController

```csharp
// Add this method to OrderController.cs
[HttpGet]
public async Task<IActionResult> GetOrderDetails(int orderId)
{
    try
    {
        var orderDetails = await _orderService.GetOrderDetails(orderId);
        return View("OrderDetails", orderDetails);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = ex.Message });
    }
}
```

### 3. Create the Service Interface and Implementation

```csharp
// IOrderService.cs
public interface IOrderService
{
    // Existing methods...
    Task<OrderDetailsViewModel> GetOrderDetails(int orderId);
}

// OrderService.cs
public async Task<OrderDetailsViewModel> GetOrderDetails(int orderId)
{
    var order = await _context.Customerorders
        .Include(o => o.Customer)
        .Include(o => o.Ordertables)
            .ThenInclude(ot => ot.Table)
                .ThenInclude(t => t.Section)
        .Include(o => o.Orderdetails)
            .ThenInclude(od => od.Item)
        .Include(o => o.Orderdetails)
            .ThenInclude(od => od.Orderdetailmodifiers)
                .ThenInclude(odm => odm.Modifier)
        .Include(o => o.Ordertaxes)
            .ThenInclude(ot => ot.Tax)
        .Include(o => o.Invoices)
        .FirstOrDefaultAsync(o => o.Orderid == orderId);

    if (order == null)
    {
        return null;
    }

    var invoice = order.Invoices.FirstOrDefault();
    
    var orderDetails = new OrderDetailsViewModel
    {
        OrderId = order.Orderid,
        InvoiceNumber = invoice != null ? $"#{invoice.Invoiceid}" : "N/A",
        Status = order.Status,
        PaymentMode = order.Paymentmode,
        PaidOn = invoice?.Createddate ?? order.Createddate,
        PlacedOn = order.Createddate,
        ModifiedOn = order.Modifieddate,
        OrderDuration = (invoice?.Createddate ?? DateTime.Now) - order.Createddate,
        TotalAmount = order.Totalamount,
        SubTotal = order.Orderdetails.Sum(od => od.Quantity * od.Item.Rate),
        
        // Customer Details
        CustomerName = order.Customer?.Customername,
        Phone = order.Customer?.Phonenumber,
        Email = order.Customer?.Email,
        NumberOfPersons = order.Ordertables.FirstOrDefault()?.Table?.Capacity ?? 0,
        
        // Table Details
        TableName = order.Ordertables.FirstOrDefault()?.Table?.Tablename ?? "N/A",
        SectionName = order.Ordertables.FirstOrDefault()?.Table?.Section?.Sectionname ?? "N/A",
        
        // Order Items
        OrderItems = order.Orderdetails.Select((od, index) => new OrderItemViewModel
        {
            SrNo = index + 1,
            ItemName = od.Item.Itemname,
            Quantity = od.Quantity,
            Price = od.Item.Rate,
            TotalAmount = od.Quantity * od.Item.Rate,
            Modifiers = od.Orderdetailmodifiers.Select(odm => new OrderItemModifierViewModel
            {
                ModifierName = odm.Modifier?.Modifiername,
                Quantity = 1, // Assuming 1 quantity per modifier
                Price = odm.Modifier?.Rate ?? 0,
                TotalAmount = odm.Modifier?.Rate ?? 0
            }).ToList()
        }).ToList(),
        
        // Taxes
        Taxes = order.Ordertaxes.Select(ot => new OrderTaxViewModel
        {
            TaxName = ot.Tax?.Taxname,
            TaxValue = ot.Taxvalue
        }).ToList()
    };

    return orderDetails;
}
```

### 4. Update the JavaScript in Orders.cshtml

```javascript
// Update the eye icon click handler
$('tbody').on('click', '.ordereye', function() {
    const orderId = $(this).closest('tr').find('td:first').text();
    window.location.href = `/Order/GetOrderDetails?orderId=${orderId}`;
});
```

### 5. Update the OrderDetails.cshtml View

```html
@model Entity.ViewModel.OrderDetailsViewModel
@{
    Layout = "~/Views/Shared/_SecondLayout.cshtml";
    ViewData["Title"] = "OrderDetails";
}

<div class="row-11 d-flex align-items-center justify-content-center">
    <div class="row" style="width: 1000px;">
        <div class="text-Users-title my-3 mx-2 fs-2 col-6">
            Order-Details
        </div>
        <div class="col my-3 text-align-right">
            <button class="btn btn-primary" onclick="window.history.back()">
                Back
            </button>
        </div>
        <div class=" col-12 my-3 bg-white table-shadow p-2">
            <div class="row-12 d-flex w-100 align-items-center">
                <p class=" col-3 fs-4 ">Order Summary</p>
                <p class="order-status fs-5 col-2">@Model.Status</p>
                <div class="col-7 d-flex justify-content-end p-2">
                    <button class="row px-3 p-2 send-button export-button" onclick="exportOrderDetails(@Model.OrderId)">
                        <div class=" d-flex">
                            <i class=" fa-solid fa-file-export my-2"></i>
                            <p class=" my-1">Export</p>
                        </div>
                    </button>
                </div>
            </div>
            <div class="row">
                <div class="col-2">
                    Invoice No :
                </div>
                <div class="col-1">
                    @Model.InvoiceNumber
                </div>
            </div>
            <div class="row w-100">
                <div class="col-2 w-auto">
                    Paid On:
                </div>
                <div class="col-1 w-auto">
                    @Model.PaidOn.ToString("yyyy-MM-dd HH:mm")
                </div>
                <div class="col-2 w-auto">
                    Placed On:
                </div>
                <div class="col-1 w-auto">
                    @Model.PlacedOn.ToString("yyyy-MM-dd HH:mm")
                </div>
                <div class="col-2 w-auto">
                    Modified On:
                </div>
                <div class="col-1 w-auto">
                    @(Model.ModifiedOn?.ToString("yyyy-MM-dd HH:mm") ?? "N/A")
                </div>
                <div class="col-2 w-auto">
                    Order Duration:
                </div>
                <div class="col-1 w-auto">
                    @Model.OrderDuration.ToString(@"hh\:mm")
                </div>
            </div>
        </div>
        <div class="col-6 bg-white table-shadow">
            <div class="row d-flex">
                <i class="fa-regular fa-user col"></i>
                <p>
                    Customer-Details
                </p>
            </div>
            <div class="row">
                <p class="col">Name: @Model.CustomerName</p>
            </div>
            <div class="row">
                <p class="col">Phone: @Model.Phone</p>
            </div>
            <div class="row">
                <p class="col">No. Of Person: @Model.NumberOfPersons</p>
            </div>
            <div class="row">
                <p class="col">Email : @Model.Email</p>
            </div>
        </div>
        <div class="col-6 bg-white table-shadow">
            <div>
                <i class="fa-solid fa-chair"></i>
                <p>
                    Table Details
                </p>
            </div>
            <div class="row">
                <p class="col">Table: @Model.TableName</p>
            </div>
            <div class="row">
                <p class="col">Section: @Model.SectionName</p>
            </div>
        </div>
        <div class="col-12 my-2 bg-white table-shadow">
            <p>Order Items</p>
            <div class="table-responsive row m-2">
                <table class="table ">
                    <thead>
                        <tr>
                            <th scope="col" class="text-black-50">Sr.no</th>
                            <th scope="col" class="text-black-50">Item</th>
                            <th scope="col" class="text-black-50">Quantity</th>
                            <th scope="col" class="text-black-50">Price</th>
                            <th scope="col" class="text-black-50">Total Amount</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var item in Model.OrderItems)
                        {
                            <tr>
                                <td>
                                    @item.SrNo
                                </td>
                                <td>
                                    @item.ItemName
                                    @if (item.Modifiers.Any())
                                    {
                                        <ul>
                                            @foreach (var modifier in item.Modifiers)
                                            {
                                                <li>
                                                    @modifier.ModifierName
                                                </li>
                                            }
                                        </ul>
                                    }
                                </td>
                                <td>
                                    @item.Quantity
                                    @if (item.Modifiers.Any())
                                    {
                                        <ul>
                                            @foreach (var modifier in item.Modifiers)
                                            {
                                                <li>
                                                    @modifier.Quantity
                                                </li>
                                            }
                                        </ul>
                                    }
                                </td>
                                <td>
                                    @item.Price.ToString("0.00")
                                    @if (item.Modifiers.Any())
                                    {
                                        <ul>
                                            @foreach (var modifier in item.Modifiers)
                                            {
                                                <li>
                                                    @modifier.Price.ToString("0.00")
                                                </li>
                                            }
                                        </ul>
                                    }
                                </td>
                                <td>
                                    @item.TotalAmount.ToString("0.00")
                                    @if (item.Modifiers.Any())
                                    {
                                        <ul>
                                            @foreach (var modifier in item.Modifiers)
                                            {
                                                <li>
                                                    @modifier.TotalAmount.ToString("0.00")
                                                </li>
                                            }
                                        </ul>
                                    }
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
            <div class="justify-content-end col">
                <p> SubTotal: @Model.SubTotal.ToString("0.00")</p>
                @foreach (var tax in Model.Taxes)
                {
                    <p> @tax.TaxName: @tax.TaxValue.ToString("0.00")</p>
                }
                <p> Total: @Model.TotalAmount.ToString("0.00")</p>
            </div>
        </div>
    </div>
</div>

<script>
    function exportOrderDetails(orderId) {
        window.location.href = `/Order/ExportOrderDetails?orderId=${orderId}`;
    }
</script>
```

### 6. Add Export Method for Order Details in OrderController

```csharp
[HttpGet]
public async Task<IActionResult> ExportOrderDetails(int orderId)
{
    var orderDetails = await _orderService.GetOrderDetails(orderId);
    
    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    using var package = new ExcelPackage();
    var worksheet = package.Workbook.Worksheets.Add("OrderDetails");

    // Add logo
    using (FileStream stream = new FileStream(@"D:\Project\Pizza_Shop\PizzaShopProject\images\logos\pizzashop_logo.png", FileMode.Open))
    {
        ExcelPicture excelImage = worksheet.Drawings.AddPicture("logo", stream);
        excelImage.SetPosition(2, 0, 12, 0);
        excelImage.SetSize(100, 100);
    }

    // Order Summary
    worksheet.Cells[1, 1].Value = "Order Summary";
    worksheet.Cells[1, 1].Style.Font.Bold = true;
    worksheet.Cells[1, 1].Style.Font.Size = 14;

    worksheet.Cells[2, 1].Value = "Order ID:";
    worksheet.Cells[2, 2].Value = orderDetails.OrderId;
    worksheet.Cells[3, 1].Value = "Status:";
    worksheet.Cells[3, 2].Value = orderDetails.Status;
    worksheet.Cells[4, 1].Value = "Invoice Number:";
    worksheet.Cells[4, 2].Value = orderDetails.InvoiceNumber;
    worksheet.Cells[5, 1].Value = "Payment Mode:";
    worksheet.Cells[5, 2].Value = orderDetails.PaymentMode;
    worksheet.Cells[6, 1].Value = "Total Amount:";
    worksheet.Cells[6, 2].Value = orderDetails.TotalAmount;

    // Customer Details
    worksheet.Cells[8, 1].Value = "Customer Details";
    worksheet.Cells[8, 1].Style.Font.Bold = true;
    worksheet.Cells[8, 1].Style.Font.Size = 14;

    worksheet.Cells[9, 1].Value = "Name:";
    worksheet.Cells[9, 2].Value = orderDetails.CustomerName;
    worksheet.Cells[10, 1].Value = "Phone:";
    worksheet.Cells[10, 2].Value = orderDetails.Phone;
    worksheet.Cells[11, 1].Value = "Email:";
    worksheet.Cells[11, 2].Value = orderDetails.Email;
    worksheet.Cells[12, 1].Value = "Number of Persons:";
    worksheet.Cells[12, 2].Value = orderDetails.NumberOfPersons;

    // Table Details
    worksheet.Cells[14, 1].Value = "Table Details";
    worksheet.Cells[14, 1].Style.Font.Bold = true;
    worksheet.Cells[14, 1].Style.Font.Size = 14;

    worksheet.Cells[15, 1].Value = "Table:";
    worksheet.Cells[15, 2].Value = orderDetails.TableName;
    worksheet.Cells[16, 1].Value = "Section:";
    worksheet.Cells[16, 2].Value = orderDetails.SectionName;

    // Order Items
    worksheet.Cells[18, 1].Value = "Order Items";
    worksheet.Cells[18, 1].Style.Font.Bold = true;
    worksheet.Cells[18, 1].Style.Font.Size = 14;

    var headers = new string[] { "Sr.No", "Item Name", "Quantity", "Price", "Total Amount" };
    for (int i = 0; i < headers.Length; i++)
    {
        worksheet.Cells[19, i + 1].Value = headers[i];
        worksheet.Cells[19, i + 1].Style.Font.Bold = true;
    }

    int row = 20;
    foreach (var item in orderDetails.OrderItems)
    {
        worksheet.Cells[row, 1].Value = item.SrNo;
        worksheet.Cells[row, 2].Value = item.ItemName;
        worksheet.Cells[row, 3].Value = item.Quantity;
        worksheet.Cells[row, 4].Value = item.Price;
        worksheet.Cells[row, 5].Value = item.TotalAmount;
        row++;

        foreach (var modifier in item.Modifiers)
        {
            worksheet.Cells[row, 2].Value = $"  - {modifier.ModifierName}";
            worksheet.Cells[row, 3].Value = modifier.Quantity;
            worksheet.Cells[row, 4].Value = modifier.Price;
            worksheet.Cells[row, 5].Value = modifier.TotalAmount;
            row++;
        }
    }

    // Order Summary
    worksheet.Cells[row + 2, 4].Value = "SubTotal:";
    worksheet.Cells[row + 2, 5].Value = orderDetails.SubTotal;

    foreach (var tax in orderDetails.Taxes)
    {
        worksheet.Cells[row + 3, 4].Value = $"{tax.TaxName}:";
        worksheet.Cells[row + 3, 5].Value = tax.TaxValue;
        row++;
    }

    worksheet.Cells[row + 4, 4].Value = "Total:";
    worksheet.Cells[row + 4, 5].Value = orderDetails.TotalAmount;
    worksheet.Cells[row + 4, 5].Style.Font.Bold = true;

    // Auto fit columns
    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

    var fileName = $"OrderDetails_{orderDetails.OrderId}.xlsx";
    return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
}
```

This implementation:

1. Creates a comprehensive ViewModel for order details that includes all necessary information
2. Implements the service method to fetch all related data from the database
3. Updates the view to display all order details
4. Adds export functionality for individual orders
5. Handles the click event from the orders page to show details

The solution follows the repository pattern and uses Entity Framework Core for data access. It includes proper error handling and follows best practices for ASP.NET Core MVC applications.