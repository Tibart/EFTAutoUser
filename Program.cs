using System;
using System.IO;
using System.Data.SqlClient;
using SFTPCOMINTERFACELib;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Data;


namespace SFTP_O2C
{
    class Program
    {
        static void Main(string[] args)
        {
            // Configuratie ophalen
            string sftpAdres = ConfigurationManager.AppSettings["sftpAdres"];
            int sftpPort = int.Parse(ConfigurationManager.AppSettings["sftpPort"]);
            string sftpUser = ConfigurationManager.AppSettings["sftpUser"]; ;
            string sftpPassword = ConfigurationManager.AppSettings["sftpPassword"];
            string sftpSitenaam = ConfigurationManager.AppSettings["sftpSitenaam"];
            string settingsTemplate = ConfigurationManager.AppSettings["settingsTemplate"];
            string connectionString = ConfigurationManager.ConnectionStrings["O2CDB"].ConnectionString;
            
            try
            {
                // Connect to SFTPd
                using (SftpServer sftpd = new SftpServer(sftpSitenaam, sftpAdres, sftpPort, sftpUser, sftpPassword))
                {
                    // List of users that need to be added or deleted from sftp server
                    List<SftpUser> adjustments = new List<SftpUser>();

                    // Actieve O2C users
                    var o2C = new O2C(connectionString);
                    var o2CUserList = o2C.GetSanddwebUsers();

                    // Actieve SFTP users
                    List<string> sftpUserList = sftpd.GetUsersAsList(settingsTemplate);

                    // New users: all active O2C users that are not present in sftpd.
                    var newUsers = o2CUserList.AsEnumerable().Where(r => !sftpUserList.Contains(r.Field<string>(0)));
                    foreach (var nu in newUsers)
                    {
                        adjustments.Add(sftpd.NewUser(nu));
                    }

                    // Inactieve users: all sftp users that are not active as O2C user
                    var inactiveUsers = sftpUserList.Where(u => !o2CUserList.AsEnumerable().Select(r => r.Field<string>(0)).Contains(u));
                    foreach (var iu in inactiveUsers)
                    {
                        adjustments.Add(sftpd.InactiveUser(iu));
                    }

                    // TEST
                    //Test(adjustments);
                    //Console.ReadLine();

                    // Apply adjustments to SFTP server
                    sftpd.AdjustSftpUsers(adjustments, settingsTemplate);

                    // Export current SFTP userlist as .csv
                    sftpd.ExportSftpUserList(settingsTemplate, "O2C_SFTP_logins");

                    
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                if (e.ErrorCode == -2147467259) // Username exitst more that ones in O2C DB
                {
                    Console.WriteLine(DateTime.Now + ": SFTPD User already exitst! - " + e.ToString());
                }

                Console.WriteLine(DateTime.Now + ": SFTPD undifined - " + e.ErrorCode.ToString());
            }
            catch (Exception e) // System.Runtime.InteropServices.COMException
            {
                Console.WriteLine(DateTime.Now + ": " + e.ToString());
            }
            finally
            {
                //Console.ReadLine();
            }
        }

        private static void Test(List<SftpUser> adjustments)
        {
            // TEST output
            foreach (var item in adjustments.OrderBy(k => k.Email))
            {
                if (item.Active)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(@"{0} {1} {2}", item.Active, item.Email, item.Password);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(@"{0} {1} {2}", item.Active, item.Email, item.Password);
                }
            }
        }
    }
}
