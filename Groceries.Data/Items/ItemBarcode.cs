namespace Groceries.Data;

public class ItemBarcode
{
    public ItemBarcode(Guid itemId, long barcodeData, string format)
    {
        ItemId = itemId;
        BarcodeData = barcodeData;
        Format = format;
    }

    public Guid ItemId { get; init; }
    public long BarcodeData { get; init; }
    public string Format { get; init; }
}
