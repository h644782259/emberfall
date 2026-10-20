namespace Emberfall
{
    public sealed partial class GameUI
    {
        private static ButtonRole CampRouteButtonRole(CampRouteCard route)
        {
            return route.NextAction == CampRouteAction.Skill || route.NextAction == CampRouteAction.Inventory ||
                route.NextAction == CampRouteAction.TrackCore ? ButtonRole.Navigation : ButtonRole.Action;
        }

        private void FollowCampRouteStep(CampRouteCard route,int index)
        {
            if(!session.IsInCamp)return;
            var p=session.Progression;
            switch(route.NextAction)
            {
                case CampRouteAction.Skill:OpenRouteSkill(route.NextSkill);break;
                case CampRouteAction.Inventory:panel=Panel.Inventory;break;
                case CampRouteAction.TrackCore:
                    NavigateMerchantExchange();break;
            }
            CancelMobileScroll();BlockUITransition();
        }
    }
}
