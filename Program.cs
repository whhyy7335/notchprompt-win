using Forms = System.Windows.Forms;

namespace NotchPromptWin;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        Forms.Application.EnableVisualStyles();
        Forms.Application.SetCompatibleTextRenderingDefault(false);
        Forms.Application.Run(new NotchPromptContext());
    }
}
