namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void DismissEquippedItemDetails()
        {
            dismissedRewardAnchor=entryRewardAnchor;suppressRewardHover=true;
            entryRewardSelection=entryRewardHoverKey=null;entryRewardPopupVisible=false;
            dismissedInventoryAnchor=inventoryPopupAnchor;suppressInventoryHover=true;
            inventoryComparisonOpen=false;inventoryPopupCompare=false;mobileInventoryDetail=false;
            inventoryPopupDismissed=UnityEngine.Time.frameCount;
            CancelMobileScroll();BlockUITransition();
        }
        private bool CloseMobileInventoryDetail()
        {
            // Comparison and fashion are inline bag surfaces on every device.
            if(panel!=Panel.Inventory||session.Paused)return false;
            if(inventoryFashionOpen){inventoryFashionOpen=false;}
            else if(inventoryComparisonOpen){inventoryComparisonOpen=false;}
            else return false;
            mobileInventoryDetail=false;CancelMobileScroll();BlockUITransition();return true;
        }
    }
}
