namespace BattleshipClient.Views.Renderers
{
    public interface IAsciiConsole
    {
        void Put(int x, int y, char ch); 
        void WriteText(int x, int y, string text);
    }
}
