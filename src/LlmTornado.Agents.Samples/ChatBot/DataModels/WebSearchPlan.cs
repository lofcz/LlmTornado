namespace ChatBot.States;

public struct WebSearchPlan
{
    public WebSearchItem[] items { get; set; }
    public WebSearchPlan(WebSearchItem[] items)
    {
        this.items = items;
    }
}







