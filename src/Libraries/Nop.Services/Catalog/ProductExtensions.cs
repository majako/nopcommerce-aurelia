using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Shipping.Date;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class ProductExtensions
    {
        #region Utilities

        private static string GeStockMessage(Product product, string attributesXml, ILocalizationService localizationService, IProductAttributeParser productAttributeParser, IDateRangeService dateRangeService)
        {
            if (!product.DisplayStockAvailability)
                return string.Empty;

            string stockMessage;

            var combination = productAttributeParser.FindProductAttributeCombination(product, attributesXml);
            if (combination != null)
            {
                //combination exists
                var stockQuantity = combination.StockQuantity;
                if (stockQuantity > 0)
                {
                    stockMessage = product.DisplayStockQuantity
                        ?
                        //display "in stock" with stock quantity
                        string.Format(localizationService.GetResource("Products.Availability.InStockWithQuantity"),
                            stockQuantity)
                        :
                        //display "in stock" without stock quantity
                        localizationService.GetResource("Products.Availability.InStock");
                }
                else if (combination.AllowOutOfStockOrders)
                {
                    stockMessage = localizationService.GetResource("Products.Availability.InStock");
                }
                else
                {
                    var productAvailabilityRange =
                        dateRangeService.GetProductAvailabilityRangeById(product.ProductAvailabilityRangeId);
                    stockMessage = productAvailabilityRange == null
                        ? localizationService.GetResource("Products.Availability.OutOfStock")
                        : string.Format(localizationService.GetResource("Products.Availability.AvailabilityRange"),
                            productAvailabilityRange.GetLocalized(range => range.Name));
                }
            }
            else
            {
                //no combination configured
                if (product.AllowAddingOnlyExistingAttributeCombinations)
                {
                    var productAvailabilityRange =
                        dateRangeService.GetProductAvailabilityRangeById(product.ProductAvailabilityRangeId);
                    stockMessage = productAvailabilityRange == null
                        ? localizationService.GetResource("Products.Availability.OutOfStock")
                        : string.Format(localizationService.GetResource("Products.Availability.AvailabilityRange"),
                            productAvailabilityRange.GetLocalized(range => range.Name));
                }
                else
                {
                    stockMessage = localizationService.GetResource("Products.Availability.InStock");
                }
            }
            return stockMessage;
        }

        private static string GetStockMessage(Product product, ILocalizationService localizationService, IDateRangeService dateRangeService, string stockMessage)
        {
            if (!product.DisplayStockAvailability)
                return string.Empty;

            var stockQuantity = product.GetTotalStockQuantity();
            if (stockQuantity > 0)
            {
                stockMessage = product.DisplayStockQuantity
                    ?
                    //display "in stock" with stock quantity
                    string.Format(localizationService.GetResource("Products.Availability.InStockWithQuantity"), stockQuantity)
                    :
                    //display "in stock" without stock quantity
                    localizationService.GetResource("Products.Availability.InStock");
            }
            else
            {
                //out of stock
                var productAvailabilityRange = dateRangeService.GetProductAvailabilityRangeById(product.ProductAvailabilityRangeId);
                switch (product.BackorderMode)
                {
                    case BackorderMode.NoBackorders:
                        stockMessage = productAvailabilityRange == null
                            ? localizationService.GetResource("Products.Availability.OutOfStock")
                            : string.Format(localizationService.GetResource("Products.Availability.AvailabilityRange"),
                                productAvailabilityRange.GetLocalized(range => range.Name));
                        break;
                    case BackorderMode.AllowQtyBelow0:
                        stockMessage = localizationService.GetResource("Products.Availability.InStock");
                        break;
                    case BackorderMode.AllowQtyBelow0AndNotifyCustomer:
                        stockMessage = productAvailabilityRange == null
                            ? localizationService.GetResource("Products.Availability.Backordering")
                            : string.Format(localizationService.GetResource("Products.Availability.BackorderingWithDate"),
                                productAvailabilityRange.GetLocalized(range => range.Name));
                        break;
                }
            }
            return stockMessage;
        }

        #endregion

        #region Methods
       
        /// <summary>
        /// Gets a preferred tier price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="quantity">Quantity</param>
        /// <returns>Tier price</returns>
        public static TierPrice GetPreferredTierPrice(this Product product, Customer customer, int storeId, int quantity)
        {
            if (!product.HasTierPrices)
                return null;

            //get actual tier prices
            var actualTierPrices = product.TierPrices.OrderBy(price => price.Quantity).ToList()
                .FilterByStore(storeId)
                .FilterForCustomer(customer)
                .FilterByDate()
                .RemoveDuplicatedQuantities();

            //get the most suitable tier price based on the passed quantity
            var tierPrice = actualTierPrices.LastOrDefault(price => quantity >= price.Quantity);
            return tierPrice;
        }
        
        /// <summary>
        /// Finds a related product item by specified identifiers
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="productId1">The first product identifier</param>
        /// <param name="productId2">The second product identifier</param>
        /// <returns>Related product</returns>
        public static RelatedProduct FindRelatedProduct(this IList<RelatedProduct> source,
            int productId1, int productId2)
        {
            foreach (var relatedProduct in source)
                if (relatedProduct.ProductId1 == productId1 && relatedProduct.ProductId2 == productId2)
                    return relatedProduct;
            return null;
        }

        /// <summary>
        /// Finds a cross-sell product item by specified identifiers
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="productId1">The first product identifier</param>
        /// <param name="productId2">The second product identifier</param>
        /// <returns>Cross-sell product</returns>
        public static CrossSellProduct FindCrossSellProduct(this IList<CrossSellProduct> source,
            int productId1, int productId2)
        {
            foreach (var crossSellProduct in source)
                if (crossSellProduct.ProductId1 == productId1 && crossSellProduct.ProductId2 == productId2)
                    return crossSellProduct;
            return null;
        }

        /// <summary>
        /// Formats the stock availability/quantity message
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributesXml">Selected product attributes in XML format (if specified)</param>
        /// <param name="localizationService">Localization service</param>
        /// <param name="productAttributeParser">Product attribute parser</param>
        /// <param name="dateRangeService">Date range service</param>
        /// <returns>The stock message</returns>
        public static string FormatStockMessage(this Product product, string attributesXml,
            ILocalizationService localizationService, IProductAttributeParser productAttributeParser, IDateRangeService dateRangeService)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (localizationService == null)
                throw new ArgumentNullException(nameof(localizationService));

            if (productAttributeParser == null)
                throw new ArgumentNullException(nameof(productAttributeParser));

            if (dateRangeService == null)
                throw new ArgumentNullException(nameof(dateRangeService));

            var stockMessage = string.Empty;

            switch (product.ManageInventoryMethod)
            {
                case ManageInventoryMethod.ManageStock:
                    stockMessage = GetStockMessage(product, localizationService, dateRangeService, stockMessage);
                    break;
                case ManageInventoryMethod.ManageStockByAttributes:
                    stockMessage = GeStockMessage(product, attributesXml, localizationService, productAttributeParser, dateRangeService);
                    break;
            }

            return stockMessage;
        }

        /// <summary>
        /// Indicates whether a product tag exists
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="productTagId">Product tag identifier</param>
        /// <returns>Result</returns>
        public static bool ProductTagExists(this Product product,
            int productTagId)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var result = product.ProductTags.ToList().Find(pt => pt.Id == productTagId) != null;
            return result;
        }

        /// <summary>
        /// Get a list of allowed quantities (parse 'AllowedQuantities' property)
        /// </summary>
        /// <param name="product">Product</param>
        /// <returns>Result</returns>
        public static int[] ParseAllowedQuantities(this Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var result = new List<int>();
            if (!string.IsNullOrWhiteSpace(product.AllowedQuantities))
            {
                product.AllowedQuantities
                    .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .ToList()
                    .ForEach(qtyStr =>
                    {
                        if (int.TryParse(qtyStr.Trim(), out int qty))
                        {
                            result.Add(qty);
                        }
                    });
            }

            return result.ToArray();
        }

        /// <summary>
        /// Get total quantity
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="useReservedQuantity">
        /// A value indicating whether we should consider "Reserved Quantity" property 
        /// when "multiple warehouses" are used
        /// </param>
        /// <param name="warehouseId">
        /// Warehouse identifier. Used to limit result to certain warehouse.
        /// Used only with "multiple warehouses" enabled.
        /// </param>
        /// <returns>Result</returns>
        public static int GetTotalStockQuantity(this Product product, 
            bool useReservedQuantity = true, int warehouseId = 0)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (product.ManageInventoryMethod != ManageInventoryMethod.ManageStock)
            {
                //We can calculate total stock quantity when 'Manage inventory' property is set to 'Track inventory'
                return 0;
            }

            if (!product.UseMultipleWarehouses)
                return product.StockQuantity;

            var pwi = product.ProductWarehouseInventory;
            if (warehouseId > 0)
            {
                pwi = pwi.Where(x => x.WarehouseId == warehouseId).ToList();
            }
            var result = pwi.Sum(x => x.StockQuantity);
            if (useReservedQuantity)
            {
                result = result - pwi.Sum(x => x.ReservedQuantity);
            }

            return result;
        }

        /// <summary>
        /// Get number of rental periods (price ratio)
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Number of rental periods</returns>
        public static int GetRentalPeriods(this Product product,
            DateTime startDate, DateTime endDate)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (!product.IsRental)
                return 1;

            if (startDate.CompareTo(endDate) >= 0)
                return 1;

            int totalPeriods;
            switch (product.RentalPricePeriod)
            {
                case RentalPricePeriod.Days:
                {
                    var totalDaysToRent = Math.Max((endDate - startDate).TotalDays, 1);
                    var configuredPeriodDays = product.RentalPriceLength;
                    totalPeriods = Convert.ToInt32(Math.Ceiling(totalDaysToRent/configuredPeriodDays));
                }
                    break;
                case RentalPricePeriod.Weeks:
                    {
                        var totalDaysToRent = Math.Max((endDate - startDate).TotalDays, 1);
                        var configuredPeriodDays = 7 * product.RentalPriceLength;
                        totalPeriods = Convert.ToInt32(Math.Ceiling(totalDaysToRent / configuredPeriodDays));
                    }
                    break;
                case RentalPricePeriod.Months:
                    {
                        //Source: http://stackoverflow.com/questions/4638993/difference-in-months-between-two-dates
                        var totalMonthsToRent = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;
                        if (startDate.AddMonths(totalMonthsToRent) < endDate)
                        {
                            //several days added (not full month)
                            totalMonthsToRent++;
                        }
                        var configuredPeriodMonths = product.RentalPriceLength;
                        totalPeriods = Convert.ToInt32(Math.Ceiling((double)totalMonthsToRent / configuredPeriodMonths));
                    }
                    break;
                case RentalPricePeriod.Years:
                    {
                        var totalDaysToRent = Math.Max((endDate - startDate).TotalDays, 1);
                        var configuredPeriodDays = 365 * product.RentalPriceLength;
                        totalPeriods = Convert.ToInt32(Math.Ceiling(totalDaysToRent / configuredPeriodDays));
                    }
                    break;
                default:
                    throw new Exception("Not supported rental period");
            }

            return totalPeriods;
        }

        /// <summary>
        /// Gets SKU, Manufacturer part number and GTIN
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="productAttributeParser">Product attribute service (used when attributes are specified)</param>
        /// <param name="sku">SKU</param>
        /// <param name="manufacturerPartNumber">Manufacturer part number</param>
        /// <param name="gtin">GTIN</param>
        private static void GetSkuMpnGtin(this Product product, string attributesXml, IProductAttributeParser productAttributeParser,
            out string sku, out string manufacturerPartNumber, out string gtin)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            sku = null;
            manufacturerPartNumber = null;
            gtin = null;

            if (!string.IsNullOrEmpty(attributesXml) && 
                product.ManageInventoryMethod == ManageInventoryMethod.ManageStockByAttributes)
            {
                //manage stock by attribute combinations
                if (productAttributeParser == null)
                    throw new ArgumentNullException(nameof(productAttributeParser));

                //let's find appropriate record
                var combination = productAttributeParser.FindProductAttributeCombination(product, attributesXml);
                if (combination != null)
                {
                    sku = combination.Sku;
                    manufacturerPartNumber = combination.ManufacturerPartNumber;
                    gtin = combination.Gtin;
                }
            }

            if (string.IsNullOrEmpty(sku))
                sku = product.Sku;
            if (string.IsNullOrEmpty(manufacturerPartNumber))
                manufacturerPartNumber = product.ManufacturerPartNumber;
            if (string.IsNullOrEmpty(gtin))
                gtin = product.Gtin;
        }

        /// <summary>
        /// Formats SKU
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="productAttributeParser">Product attribute service (used when attributes are specified)</param>
        /// <returns>SKU</returns>
        public static string FormatSku(this Product product, string attributesXml = null, IProductAttributeParser productAttributeParser = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            product.GetSkuMpnGtin(attributesXml, productAttributeParser, out string sku, out string _, out string _);

            return sku;
        }

        /// <summary>
        /// Formats manufacturer part number
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="productAttributeParser">Product attribute service (used when attributes are specified)</param>
        /// <returns>Manufacturer part number</returns>
        public static string FormatMpn(this Product product, string attributesXml = null, IProductAttributeParser productAttributeParser = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            product.GetSkuMpnGtin(attributesXml, productAttributeParser, out string _, out string manufacturerPartNumber, out string _);

            return manufacturerPartNumber;
        }

        /// <summary>
        /// Formats GTIN
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="productAttributeParser">Product attribute service (used when attributes are specified)</param>
        /// <returns>GTIN</returns>
        public static string FormatGtin(this Product product, string attributesXml = null, IProductAttributeParser productAttributeParser = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            product.GetSkuMpnGtin(attributesXml, productAttributeParser, out string _, out string _, out string gtin);

            return gtin;
        }

        /// <summary>
        /// Formats start/end date for rental product
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="date">Date</param>
        /// <returns>Formatted date</returns>
        public static string FormatRentalDate(this Product product, DateTime date)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (!product.IsRental)
                return null;

            return date.ToShortDateString();
        }

        /// <summary>
        /// Format base price (PAngV)
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="productPrice">Product price (in primary currency). Pass null if you want to use a default produce price</param>
        /// <param name="localizationService">Localization service</param>
        /// <param name="measureService">Measure service</param>
        /// <param name="currencyService">Currency service</param>
        /// <param name="workContext">Work context</param>
        /// <param name="priceFormatter">Price formatter</param>
        /// <returns>Base price</returns>
        public static string FormatBasePrice(this Product product, decimal? productPrice, ILocalizationService localizationService,
            IMeasureService measureService, ICurrencyService currencyService,
            IWorkContext workContext, IPriceFormatter priceFormatter)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (localizationService == null)
                throw new ArgumentNullException(nameof(localizationService));
            
            if (measureService == null)
                throw new ArgumentNullException(nameof(measureService));

            if (currencyService == null)
                throw new ArgumentNullException(nameof(currencyService));

            if (workContext == null)
                throw new ArgumentNullException(nameof(workContext));

            if (priceFormatter == null)
                throw new ArgumentNullException(nameof(priceFormatter));

            if (!product.BasepriceEnabled)
                return null;

            var productAmount = product.BasepriceAmount;
            //Amount in product cannot be 0
            if (productAmount == 0)
                return null;
            var referenceAmount = product.BasepriceBaseAmount;
            var productUnit = measureService.GetMeasureWeightById(product.BasepriceUnitId);
            //measure weight cannot be loaded
            if (productUnit == null)
                return null;
            var referenceUnit = measureService.GetMeasureWeightById(product.BasepriceBaseUnitId);
            //measure weight cannot be loaded
            if (referenceUnit == null)
                return null;

            productPrice = productPrice.HasValue ? productPrice.Value : product.Price;

            var basePrice = productPrice.Value /
                //do not round. otherwise, it can cause issues
                measureService.ConvertWeight(productAmount, productUnit, referenceUnit, false) * 
                referenceAmount;
            var basePriceInCurrentCurrency = currencyService.ConvertFromPrimaryStoreCurrency(basePrice, workContext.WorkingCurrency);
            var basePriceStr = priceFormatter.FormatPrice(basePriceInCurrentCurrency, true, false);

            var result = string.Format(localizationService.GetResource("Products.BasePrice"),
                basePriceStr, referenceAmount.ToString("G29"), referenceUnit.Name);
            return result;
        }

        #endregion
    }
}
