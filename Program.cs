using System;
using System.Windows.Forms;
using TatankaDefender; // Aggiungi questa riga

namespace TatankaDefender
{

     

    static class Program
    {

    private static Mutex mutex = null;

    public static bool IsAlreadyRunning()
    {
        const string appName = "TatankaDefenderUniqueName";
        bool createdNew;

        // Tenta di creare un Mutex con un nome unico
        mutex = new Mutex(true, appName, out createdNew);

        if (!createdNew)
        {
            // Se createdNew è false, significa che il Mutex esiste già
            // e quindi un'istanza del programma è già attiva
            return true;
        }
        return false;
    }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }    
    }
}
