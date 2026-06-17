namespace SlotRogue.UI.GameFlow
{
    public interface IEnemyCombatVisual
    {
        void PlayIdle();

        void PlayAction(string actionName);
    }
}
