using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

class Program

{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            //MessageBox.Show(Path.GetFileName(Assembly.GetExecutingAssembly().Location) + " " + String.Join(" ", args));
            GoogleTestBootstrap.TestMain(true, args, new ClassicUnitSuiteInfo());
        }
        catch (Exception ex)
        {
            String lf = "\r\n";
            MessageBox.Show("Error: " + ex.Message + lf + lf + "call stack: " + lf + lf + ex.StackTrace);
        }

        return 0;
    }



}

