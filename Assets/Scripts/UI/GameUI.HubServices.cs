using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 hubServiceScroll;
        private void DrawHubEquipmentService()
        {
            bool smith=inventoryHubNpc==HubNpcKind.Blacksmith&&HubServicesAvailable;
            if(smith){DrawSmithService();return;}
            DrawMerchantService();
        }
    }
}
