public interface IEngravable
{
    bool TryEngrave(EngravingItem item);
    void RemoveEngraving(bool returnToInventory);
}
