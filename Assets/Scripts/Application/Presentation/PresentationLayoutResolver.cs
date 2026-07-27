namespace WuxiaRoguelite.Application.Presentation
{
    public enum PresentationLayoutMode
    {
        Portrait,
        Landscape
    }

    public static class PresentationLayoutResolver
    {
        public static PresentationLayoutMode Resolve(int width, int height)
        {
            return width > height
                ? PresentationLayoutMode.Landscape
                : PresentationLayoutMode.Portrait;
        }
    }
}
