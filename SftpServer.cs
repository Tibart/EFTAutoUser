using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using SFTPCOMINTERFACELib;

namespace SFTP_O2C
{
    class SftpServer : IDisposable
    {
        private CIServer _server;
        private CISite _site;
        
        public SftpServer(string siteName, string address, int port, string userName, string passWord)
        {
            // Connecto to SFTP server
            _server = new CIServer();
            _server.Connect(address, port, userName, passWord);
            
            // Get site object
            _site = new CISite();
            CISites sites = _server.Sites();
            for (int i = 0; i < sites.Count(); i++)
            {
                CISite s = sites.Item(i);
                if (s.Name == siteName)
                {
                    _site = s;
                    break;
                }
            }
        }
        
        public List<string> GetUsersAsList(string settingsTemplate)
        {
            var users = _site.GetSettingsLevelUsers(settingsTemplate);
            List<string> userList = new List<string>();
            foreach (var u in users)
                userList.Add(u);
            return userList;
        }
        public SftpUser NewUser(DataRow dr)
        {
            return new SftpUser
            {
                FullName = String.Format("{0} ({1}) - {2}", dr.Field<string>(2), dr.Field<string>(3), dr.Field<string>(1)),
                Email = dr.Field<string>(0),
                Active = true,
                Path = dr.Field<string>(4),
                Folder = dr.Field<string>(4).Substring((_site.GetRootFolder().Length) - 1).Replace(@"\", "/"),
                Password = _site.CreateComplexPassword(dr.Field<string>(0)),
            };
        }
        public SftpUser InactiveUser(string user)
        {
            var userSettings = _site.GetUserSettings(user);
            return new SftpUser
            {
                FullName = userSettings.FullName,
                Email = user,
                Active = false,
                Path = _site.GetRootFolder() + userSettings.GetHomeDirString().Substring(1).Replace("/", @"\"),
                Folder = userSettings.GetHomeDirString(),
                Password = userSettings.Custom1,
            };
        }
        public void AdjustSftpUsers(List<SftpUser> users, string settingsTemplate)
        {
            foreach (var u in users)
            {
                if (u.Active)
                {
                    if (!Directory.Exists(u.Path))
                        Directory.CreateDirectory(u.Path);

                    // Create user
                    _site.CreateUserEx2(new CINewUserData{
                        FullName = u.FullName,
                        Login = u.Email,
                        Email = u.Email,
                        Password = u.Password,
                        PasswordType = 0,
                        Description = "",
                        CreateHomeFolder = false,
                        FullPermissionsForHomeFolder = true,
                        SettingsLevel = settingsTemplate,
                        TwoFactorAuthentication = SFTPAdvBool.abInherited
                    });
                    
                    // Set settings
                    var settings = _site.GetUserSettings(u.Email);
                    settings.SetHomeDirString(u.Folder);
                    settings.SetHomeDir(SFTPAdvBool.abTrue);
                    settings.Custom1 = u.Password;
                    
                    // Set permissions
                    var permissions = _site.GetBlankPermission(u.Folder, u.Email);
                    permissions.DirCreate = true; 
                    permissions.DirDelete = true;
                    permissions.DirList = true;
                    permissions.DirShowHidden = true;
                    permissions.DirShowInList = true;
                    permissions.DirShowReadOnly = true;
                    permissions.FileAppend = true;
                    permissions.FileDelete = true;
                    permissions.FileDownload = true;
                    permissions.FileRename = true;
                    permissions.FileUpload = true;
                    _site.SetPermission(permissions, false);

                    // Print added SFTP user
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(@"(+) {0}; {1}", u.Email, u.Folder);
                }
                else
                {
                    // Remove user
                    _site.RemoveUser(u.Email);

                    // Remove folder if there are no more active users
                    var folderUserCount = Enumerable.Count(_site.GetFolderPermissions(u.Folder));
                    if (folderUserCount == 1 && Directory.Exists(u.Path))
                        Directory.Delete(u.Path);

                    // Print removed SFTP user
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(@"(-) {0}; {1}", u.Email, u.Folder);
                }
            }
        }
        public void ExportSftpUserList(string settingsTemplate, string fileName)
        {
            // Determen export path
            var exportPath = String.Concat(
                _site.GetRootFolder(), 
                _site.GetSettingsLevelSettings(settingsTemplate).GetHomeDirString(), fileName, 
                ".csv");

            if(File.Exists(exportPath))
                File.Delete(exportPath);

            using (StreamWriter sw = File.AppendText(exportPath))
            {
                // Column names
                sw.WriteLine("Fullname;Login;Password;Path");
                // Rows
                foreach (var u in _site.GetSettingsLevelUsers(settingsTemplate))
                {
                    var userSettings = _site.GetUserSettings(u);
                    sw.WriteLine("{0};{1};{2};{3}", 
                        userSettings.FullName, 
                        u, 
                        userSettings.Custom1, 
                        _site.GetRootFolder() + userSettings.GetHomeDirString().Substring(1).Replace("/", @"\")
                    );
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _server.Close();
            }
        }
        ~SftpServer()
        {
            Dispose(false);
        }
    }
    class SftpUser
    {
        // Properties
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool Active { get; set; }
        public string Path { get; set; }
        public string Folder { get; set; }
        public string Password { get; set; }

        // Construtors
        public SftpUser()
        {
        }
        public SftpUser(CISite site, string fullName, string email, bool active, string path)
        {
            FullName = fullName;
            Email = email;
            Active = active;
            Path = path;
            Folder = path.Substring((site.GetRootFolder().Length) - 1).Replace(@"\", "/");
            Password = site.CreateComplexPassword(Email);
        }
    }
}
