/*
 * Version:
 * $Id: WebServer.cs,v 1.17 2007/02/22 02:32:16 kjb9089 Exp $
 * 
 * Revisions:
 * $Log: WebServer.cs,v $
 * Revision 1.17  2007/02/22 02:32:16  kjb9089
 * - Final Version before final submission
 *
 * Revision 1.16  2007/02/07 04:26:46  kjb9089
 * - Fixed as many fricken errors as I could. Soooo frustrating.
 *
 * Revision 1.15  2007/02/06 18:08:19  kjb9089
 * - Fixed file upload-related issues
 *
 * Revision 1.14  2007/02/05 05:54:51  kjb9089
 * - Lots and lots of growling.
 *
 * Revision 1.13  2007/02/05 05:06:01  kjb9089
 * - Fixed errors involved with logging in and with profile updates.
 *
 * Revision 1.12  2007/02/04 20:04:44  kjb9089
 * - Added CheckConfigFile() method that updates the server configuration if the
 *   config file has been updated
 * - Removed ban on Opera (seems to work in Opera now :))
 *
 * Revision 1.11  2007/02/03 22:16:44  kjb9089
 * - Corrected errors related to form input
 * - Added support for file upload and thus avatars
 * - Added more error codes
 *
 * Revision 1.10  2007/02/02 15:36:17  kjb9089
 * - Start of adding better multipart forms (will lead to the ability to upload files)
 *
 * Revision 1.9  2007/01/25 17:27:07  kjb9089
 * - Optimized code a bit (not perfectly, but better)
 * - Added admin rights (must edit .xml file to enable)
 * - Added more server-side parsing abilities
 *
 * Revision 1.8  2007/01/22 18:42:57  kjb9089
 * - Fixed Login feature
 * - Added Logout feature
 *
 * Revision 1.7  2007/01/22 03:15:39  kjb9089
 * - Added cookie support
 *
 * Revision 1.6  2007/01/15 22:31:24  kjb9089
 * - Added comments
 * - Readied for 2nd deadline
 *
 * Revision 1.5  2007/01/13 18:40:00  kjb9089
 * - Added more error pages
 *   - Removed Opera support completely, until a work-around their way of sending
 *     form data is developed
 * - Fixed repeated form submission errors.
 *
 * Revision 1.4  2007/01/12 18:20:54  kjb9089
 * - Added BGException class to assist in showing error pages.
 *   - When an error occurs, the exception is thrown with the specified error
 *     code.
 *   - This error code is used to send the proper error page to the client.
 *
 * Revision 1.3  2007/01/12 15:27:20  kjb9089
 * - Added basic error pages (more later)
 * - Added more configuration variables
 * - Added function to check server names for distribution of .xml files
 * - Fixed GetFileContents() to check for empty filenames
 * - Fixed Keep-Alive functionality
 *
 * Revision 1.2  2007/01/11 20:03:11  kjb9089
 * - Cleaned up code.
 * - Added Keep-Alive feature
 * - Added various configuration variables
 *
 * Revision 1.1  2007/01/07 06:43:09  kjb9089
 * - Basic web server
 *   - Serves HTML webpages
 *   - Manipulating files of type .bgw based on form input
 *   - Loads and stores configuration information in a .xml file
 *   - Outputs basic error pages
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace BattleGrounds {
   /// <summary>
   /// A web server program geared towards implementing commands and
   /// configuration of BattleGrounds.
   /// <list type="bullet">
   /// <item>
   /// <term>Authors:</term>
   /// <description>Kenneth Beck</description>
   /// </item>
   /// </list>
   /// </summary>
   internal class WebServer {
      #region variables
      // Default files/directories
      internal const string DEFAULT_CONFIG_PATH = "config/";
      internal const string DEFAULT_CONFIG_FILE = "WebServer.xml";
      internal const string DEFAULT_ERR_PATH = "err/";
      internal const string DEFAULT_ERR_FILE = "WebErr.xml";
      internal const string DEFAULT_WORKING_DIR = "public_html/";
      internal const string DEFAULT_USER_DIR = "users/";
      internal static readonly string[] DEFAULT_AVATAR_TYPES = new string[] {
         "gif",
         "jpg",
         "ico",
         "bmp"
      };

      // Error Page Information
      internal const string DEFAULT_ERR_PAGE_LAYOUT =
         @"
<html>
<head>
<title>An Error Has Occurred</title>
<link rel=""stylesheet"" href=""/BGStyle.css"" />
</head>
<body>
%INCLUDEFILE<file>/menu.bgw</file>%/INCLUDEFILE
<table align=""center"">
   <tr>
      <td align=""center""><h2>$ERRCODE</h2></td>
   </tr>
   <tr>
      <td align=""center"">$ERRMSG</td>
   </tr>
</table>
</body>
</html>";

      internal static readonly string[] DEFAULT_ERR_CODES = new string[] {
         "000 Some Error Occurred.",
         "401 Unauthorized",
         "404 $0 Not Found",
         "408 Request Timed Out",
         "412 Precondition Failed",
         "450 Maximum Requests Exceeded",
         "451 File Too Large",
         "452 Image Too Large",
         "460 Action Not Supported",
         "470 User Name Required",
         "471 Improper File Type",
         "472 Password Incorrect",
         "473 Password Required",
         "474 Passwords Not Equal",
         "475 User Already Exists",
         "476 $0 Improperly Formatted", 
         "477 $0 Too Short",
         "478 $0 Too Long",
         "479 Login Failed",
         "480 Already Logged In",
         "490 Must Be Logged In",
         "500 Some Error Occurred",
         "550 Socket Exception",
         "551 Image Upload Error"
      };
      internal static readonly string[] DEFAULT_ERR_MSGS = new string[] {
         "An undocumented error has occurred. Sorry for the inconvenience.",
         "You are not authorized to view this page.",
         "The requested $0 <b>($1)</b> was not found on this server.",
         "The current connection to the server has timed out. Please reconnect.",
         "Uploaded files must be no larger than $0kb.",
         "The maximum number of requests (<b>$0</b>) for the current connection "+
            "have been exceeded. Please wait at least <b>$1</b> seconds before continuing.",
         "You are trying to upload a file that is too large for this server. Max file upload size is: $0",
         "The dimensions of the uploaded picture cannot exceed $0.",
         "This form's action (<b>$0</b>) is not supported, or no action was specified. Please notify the webmaster.",
         "The User Name field is required, but is empty. Please fix.",
         "The file you attempted to upload is not of the proper format. Supported formats: <b>$0</b>",
         "The password you typed is incorrect. Please correct.",
         "The password field is required, yet it is empty.",
         "The passwords you have entered do not match.",
         "The User <b>($0)</b> already exists in our database.",
         "$0 not properly formatted. $0 cannot contain the following characters/symbols: <b>$1</b>",
         "$0 must be at least <b>$1</b> characters in length.",
         "$0 cannot be any longer than <b>$1</b> characters in length.",
         "The User <b>$0</b> with the given password combination was not found on this server.",
         "You are already logged in on this server.",
         "You must be logged in to view this page.",
         "An uncaught exception has occurred. If you were submitting a form, please " +
            "go back and resubmit it. This should fix the issue.",
         "A socket exception has occurred. Please resubmit your form."+
            "If this problem persists, please contact the web master.",
         "Failed to upload the picture properly. Please try again."
      };

      // Default Webpage Files (in case they don't exist)
      internal static readonly string[] FILE_NAMES = new string[] {
         "index.bgw",
         "menu.bgw",
         "BGStyle.css",
         "addUser.htm",
         "viewUser.bgw",
         "editUser.bgw",
         "logIn.bgw"
      };

      internal static readonly string[] FILE_CONTENTS = new string[]{
         @"
<html>
<head>
<title>BattleGrounds</title>
<link rel=""stylesheet"" href=""BGStyle.css"" />
</head>
<body>
%INCLUDEFILE<file>menu.bgw</file>%/INCLUDEFILE
</body>
</html>",
         
         @"
<table align=""center"" border=""0"" width=""600"" style=""text-align:center;"">
   <tr>
      %LOGGEDIN
      <td align=""center""><b>Logged In As:</b><br>$NICK</td>
      <td><a href=""editUser.bgw"">Edit User</a></td>
      %/LOGGEDIN
      <td><a href=""addUser.bgw"">Add A User</a></td>
      %LOGGEDOUT
      <td><a href=""logIn.bgw"">Log In</a></td>
      %/LOGGEDOUT
      %LOGGEDIN
      <td><a href=""index.bgw?action=logOut"">Log Out</a></td>
      %/LOGGEDIN
      <td><a href=""index.bgw"">Home</a></td>
   </tr>
</table>
<br><br>",

         @"
body{
   background-color: #000000;
   color: #FFFFFF;
}
a{font-weight: bold;}
a:link{color: 0000FF;}
a:visited{color: FF0000;}",

         @"
<html>
<head>
<title>Add User</title>
<link rel=""stylesheet"" href=""BGStyle.css"" />
</head>
<body>%INCLUDEFILE<file>menu.bgw</file>%/INCLUDEFILE
<form method=""post"" action=""viewUser.bgw"">
<input type=""hidden"" name=""action"" value=""adduser"">
<table align=""center"">   
   <tr>
      <td>Full Name: </td>
      <td><input type=""text"" name=""fullname""></td>
   </tr>
   <tr>
      <td>Nickname: </td>
      <td><input type=""text"" name=""nick""></td>
   </tr>
   <tr>
      <td>Password: </td>
      <td><input type=""password"" name=""password""></td>
   </tr>
   <tr>
      <td>Re-Type:</td>
      <td><input type=""password"" name=""passCheck""></td>
   </tr>
   <tr>
      <td>Info:</td>
      <td><textarea name=""info"">I am a n00b.</textarea>
   <tr>
      <td colspan=""2"" align=""center""><input type=""submit"" value=""Add User""></td>
   </tr>
</table>
</form>
</body>
</html>",
         
         @"
<html>
<head>
<title>View User</title>
<link rel=""stylesheet"" href=""BGStyle.css"" />
</head>
<body>
%INCLUDEFILE<file>menu.bgw</file>%/INCLUDEFILE
<table align=""center"">
   <tr>
      <td colspan=""2"" align=""center"">$MSG</td>
   </tr>
   <tr>
      <td><b>Full Name:</b></td>
      <td>$FULLNAME</td>
   </tr>
   <tr>
      <td><b>Nickname:</b></td>
      <td>$NICK</td>
   </tr>
   <tr>
      <td><b>Info:</b></td>
      <td>$INFO</td>
   </tr>
</table>
</body>
</html>",

         @"
<html>
<head>
<title>Log In</title>
<link rel=""stylesheet"" href=""BGStyle.css"" />
</head>
<body>
%INCLUDEFILE<file>menu.bgw</file>%/INCLUDEFILE
<form method=""post"" action=""viewUser.bgw"">
<input type=""hidden"" name=""action"" value=""updateUser"">
<table align=""center"">
   <tr>
      <td>Full Name: </td>
      <td><input type=""text"" name=""fullname"" value=""$FULLNAME""></td>
   </tr>
   <tr>
      <td>Info: </td>
      <td><textarea name=""info"">$INFO</textarea></td>
   </tr>
   <tr>
      <td colspan=""2"" align=""center""><input type=""submit"" value=""Update Info""></td>
   </tr>
</table>
</form>
</body>
</html>",
         
         @"
<html>
<head>
<title>Log In</title>
<link rel=""stylesheet"" href=""BGStyle.css"" />
</head>
<body>
%INCLUDEFILE<file>menu.bgw</file>%/INCLUDEFILE
<form method=""post"" action=""index.bgw"">
<input type=""hidden"" name=""action"" value=""logIn"">
<table align=""center"">
   <tr>
      <td>Nickname: </td>
      <td><input type=""text"" name=""username""></td>
   </tr>
   <tr>
      <td>Password: </td>
      <td><input type=""password"" name=""password""></td>
   </tr>
   <tr>
      <td colspan=""2"" align=""center""><input type=""submit"" value=""Log In""></td>
   </tr>
</table>
</form>
</body>
</html>"
      };

      // Default Game Server (only server that can receive user .xml files)
      internal const string DEFAULT_BG_SERVER = "localhost";

      // Default server header information
      internal const string DEFAULT_MIME_HEADER = "text/html";
      internal const string DEFAULT_SERVER_NAME = "BGWebServer";
      internal const int DEFAULT_LISTEN_PORT = 80;
      internal const int DEFAULT_MAX_FILE_UPLOAD_SIZE = 1024 * 25;
      internal const int DEFAULT_KEEP_ALIVE = 15;
      internal const int DEFAULT_MAX_REQUESTS = 100;

      // Default file names for when directories are requested with no file name.
      internal readonly string[] DEFAULT_FILE_NAMES = new string[] {
         "index.bgw",
         "index.htm",
         "index.html"
      };

      internal static readonly string[] DEFAULT_MODEL_FILES = new string[] {
         "",
         "tinyred.x",
         "tinygreen.x",
         "tinyblue.x"
      };

      internal static readonly string[] DEFAULT_MODELS = new string[] {
         "Regular",
         "Red",
         "Green",
         "Blue"
      };

      internal static string[] modelFiles = DEFAULT_MODEL_FILES;
      internal static string[] models = DEFAULT_MODELS;

      internal static string workingDir = DEFAULT_WORKING_DIR;
      internal static string WorkingDir { get { return workingDir; } }
      internal static string userDir = DEFAULT_USER_DIR;
      internal static string UserDir { get { return userDir; } }

      internal static string errPageLayout = DEFAULT_ERR_PAGE_LAYOUT;
      internal static string[] errCodes = DEFAULT_ERR_CODES;
      internal static string[] errMsgs = DEFAULT_ERR_MSGS;

      internal static string[] bgServers = new string[] {
         DEFAULT_BG_SERVER
      };

      internal static string serverName = DEFAULT_SERVER_NAME;
      internal static string defaultMIME = DEFAULT_MIME_HEADER;
      internal static string[] avatarTypes = DEFAULT_AVATAR_TYPES;
      internal static string[] AvatarTypes { get { return avatarTypes; } }
      internal int listenPort = DEFAULT_LISTEN_PORT;
      internal static int maxFileUploadSize = DEFAULT_MAX_FILE_UPLOAD_SIZE;
      internal static int keepAlive = DEFAULT_KEEP_ALIVE;
      internal static int maxRequests = DEFAULT_MAX_REQUESTS;
      internal static int MaxRequests { get { return maxRequests; } }

      internal static string[] defaultFileNames;
      #endregion

      internal TcpListener theListener;

      internal Thread listenThread;

      internal static WebServer server;
      internal WebConnection webConn;
      internal static DateTime startTime;

      /// <summary>
      /// Construct the WebServer
      /// </summary>
      internal WebServer() {
         string startIn = Assembly.GetExecutingAssembly().Location;
         startIn = startIn.Substring(0, startIn.LastIndexOf('\\'));
         Directory.SetCurrentDirectory(startIn);
         LoadConfiguration();

         try {
            theListener = new TcpListener(IPAddress.Any, listenPort);
            Console.WriteLine("Server started on " + theListener.LocalEndpoint);
            theListener.Start();
            startTime = DateTime.Now;
            webConn = new WebConnection();

            listenThread = new Thread(new ThreadStart(Listen));
            listenThread.Start();

         } catch (SocketException) {
            Console.WriteLine("Socket Exception: Please change the listen port.");
         }
      }

      /// <summary>
      /// Checks the configuration file every second to see if it has been updated.
      /// If it has been, it reloads the configuration file.
      /// </summary>
      internal static void CheckConfigFile() {
         string cnf = DEFAULT_CONFIG_PATH + DEFAULT_CONFIG_FILE;
         DateTime lastTime = File.GetLastWriteTime(cnf);
         while (true) {
            if (File.GetLastWriteTime(cnf) != lastTime) {
               try {
                  Console.Clear();
               } catch (IOException) { }
               Console.WriteLine("Config File Updated. Reloading.");
               if (server != null) {
                  server.Close();
                  server = new WebServer();
               }
               lastTime = File.GetLastWriteTime(cnf);
            }
            Thread.Sleep(1000);
         }
      }


      internal void Close() {
         if (listenThread != null) {
            listenThread.Abort();
            listenThread = null;
         }
         if (webConn != null) {
            webConn.Close();
            webConn = null;
         }

         if (theListener != null) {
            theListener.Stop();
            theListener = null;
         }
      }

      /// <summary>
      /// Constructs the default config file
      /// </summary>
      internal void CreateConfigFile() {
         CreateConfigFile(DEFAULT_CONFIG_PATH, DEFAULT_CONFIG_FILE);
      }

      /// <summary>
      /// Constructs the given config file
      /// using current program settings.
      /// <param name="configPath">Config File Path</param>
      /// <param name="configFile">Config File Name</param>
      /// </summary>
      internal void CreateConfigFile(string configPath, string configFile) {
         if (!Directory.Exists(configPath)) {
            Directory.CreateDirectory(configPath);
         }

         XmlTextWriter theWriter = new XmlTextWriter(configPath + configFile, null);
         theWriter.Formatting = Formatting.Indented;


         theWriter.WriteStartDocument();

         theWriter.WriteStartElement("serverconfig");

         theWriter.WriteStartElement("servername");
         theWriter.WriteString(serverName);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("workingdir");
         theWriter.WriteString(workingDir);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("defaultMIME");
         theWriter.WriteString(defaultMIME);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("defaultfilenames");
         theWriter.WriteString(string.Join(";", defaultFileNames));
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("listenport");
         theWriter.WriteString("" + listenPort);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("maxfileuploadsize");
         theWriter.WriteString("" + maxFileUploadSize);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("keepalivetimeout");
         theWriter.WriteString("" + keepAlive);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("maxrequests");
         theWriter.WriteString("" + maxRequests);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("userdirectory");
         theWriter.WriteString(userDir);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("bgservers");
         theWriter.WriteString(string.Join(";", bgServers));
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("fullnamepattern");
         theWriter.WriteString(BGUsers.FullNamePattern);
         theWriter.WriteEndElement();


         theWriter.WriteStartElement("minfullnamelength");
         theWriter.WriteString("" + BGUsers.MinFullNameLength);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("maxfullnamelength");
         theWriter.WriteString("" + BGUsers.MaxFullNameLength);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("nickpattern");
         theWriter.WriteString(BGUsers.NickPattern);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("minnicklength");
         theWriter.WriteString("" + BGUsers.MinNickLength);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("maxnicklength");
         theWriter.WriteString("" + BGUsers.MaxNickLength);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("passpattern");
         theWriter.WriteString(Uri.EscapeDataString(BGUsers.PassPattern));
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("minpasslength");
         theWriter.WriteString("" + BGUsers.MinPassLength);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("maxpasslength");
         theWriter.WriteString("" + BGUsers.MaxPassLength);
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("avatartypes");
         theWriter.WriteString(string.Join(";", avatarTypes));
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("modelfiles");
         theWriter.WriteString(string.Join(";", modelFiles));
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("modelnames");
         theWriter.WriteString(string.Join(";", models));
         theWriter.WriteEndElement();

         theWriter.WriteStartElement("picsize");
         theWriter.WriteString(BGUsers.picSize.Width + "x" + BGUsers.picSize.Height);
         theWriter.WriteEndElement();

         theWriter.WriteEndElement();

         theWriter.WriteEndDocument();
         theWriter.Close();
      }

      internal static void DisplayOutput(string output) {

      }

      /// <summary>
      /// Tells whether or not the specified
      /// client is a BGServer
      /// </summary>
      /// <param name="clientIP">IP to check</param>
      /// <returns>True if it is a BGServer, false otherwise</returns>
      internal static bool isBGServer(string clientIP) {
         for (int i = 0; i < bgServers.Length; i++) {
            if (clientIP.Equals(bgServers[i])) {
               return true;
            }
         }
         return false;
      }


      /// <summary>
      /// Method to listen for and properly handle a connection.
      /// </summary>
      internal void Listen() {
         Socket theSocket;
         while (true) {
            theSocket = theListener.AcceptSocket();
            if (theSocket.Connected) {
               WebConnection.newConnQueue.Enqueue(theSocket);
            }
            Thread.Sleep(10);
         }
      }

      /// <summary>
      /// Load configuration data, using the
      /// default configuration file
      /// </summary>
      internal void LoadConfiguration() {
         string configFile = DEFAULT_CONFIG_PATH + DEFAULT_CONFIG_FILE;
         if (!File.Exists(configFile) || File.ReadAllBytes(configFile).Length == 0) {
            Console.WriteLine("WARNING: Config File '{0}' Not Found. Loading Program Defaults.", configFile);
            SetDefaultFileNames();
            CreateConfigFile();
         }
         LoadConfiguration(configFile);
         CreateConfigFile();
      }

      /// <summary>
      /// Load configuration data from the configuration file
      /// </summary>
      /// <param name="configFile">Configuration file to use</param>
      internal void LoadConfiguration(string configFile) {
         XmlTextReader theReader = new XmlTextReader(configFile);

         theReader.MoveToContent();
         while (theReader.Read()) {
            XmlNodeType nodeType = theReader.NodeType;

            if (nodeType == XmlNodeType.Element) {
               string theString = theReader.ReadString();
               if (theString != "") {
                  switch (theReader.Name) {
                     case "servername":
                        serverName = theString;
                        Console.WriteLine("Server Name: " + serverName);
                        break;
                     case "workingdir":
                        workingDir = theString;
                        if (!Directory.Exists(workingDir)) {
                           Console.WriteLine("Created Working Directory: " + workingDir);
                           Directory.CreateDirectory(workingDir);
                        } else {
                           Console.WriteLine("Relative Working Directory: " + workingDir);
                        }
                        break;
                     case "defaultMIME":
                        defaultMIME = theString;
                        Console.WriteLine("Default MIME type: " + defaultMIME);
                        break;
                     case "defaultfilenames":
                        defaultFileNames = theString.Split(';');
                        Console.WriteLine("Default File Names: " + string.Join("; ", defaultFileNames));
                        break;
                     case "listenport":
                        listenPort = int.Parse(theString);
                        Console.WriteLine("Listen Port: " + listenPort);
                        break;
                     case "maxfileuploadsize":
                        maxFileUploadSize = int.Parse(theString);
                        Console.WriteLine("Max File Upload Size: " + maxFileUploadSize / 1024 + "kb");
                        break;
                     case "keepalivetimeout":
                        keepAlive = int.Parse(theString);
                        Console.WriteLine("Keep-Alive: " + keepAlive);
                        break;
                     case "maxrequests":
                        maxRequests = int.Parse(theString);
                        Console.WriteLine("Max Requests: " + maxRequests);
                        break;
                     case "userdirectory":
                        userDir = theString;
                        if (!Directory.Exists(userDir)) {
                           Directory.CreateDirectory(userDir);
                           Console.WriteLine("Created User Directory: " + userDir);
                        } else {
                           Console.WriteLine("User Directory: " + userDir);
                        }
                        break;
                     case "bgservers":
                        Console.Write("BG Servers: ");
                        IPHostEntry theEntry = new IPHostEntry();
                        bgServers = theString.Split(';');
                        Console.WriteLine(string.Join(", ", bgServers));
                        break;
                     case "fullnamepattern":
                        BGUsers.FullNamePattern = theString;
                        Console.WriteLine("Disallowed Username Chars: " + BGUsers.GetTestString(BGUsers.FullNamePattern));
                        break;
                     case "minfullnamelength":
                        BGUsers.MinFullNameLength = int.Parse(theString);
                        Console.WriteLine("Minimum Name Length: " + BGUsers.MinFullNameLength);
                        break;
                     case "maxfullnamelength":
                        BGUsers.MaxFullNameLength = int.Parse(theString);
                        Console.WriteLine("Maximum Name Length: " + BGUsers.MaxFullNameLength);
                        break;
                     case "nickpattern":
                        BGUsers.NickPattern = theString;
                        Console.WriteLine("Disallowed Nickname Chars: " + BGUsers.GetTestString(BGUsers.NickPattern));
                        break;
                     case "minnicklength":
                        BGUsers.MinNickLength = int.Parse(theString);
                        Console.WriteLine("Minimum Nickname Length: " + BGUsers.MinNickLength);
                        break;
                     case "maxnicklength":
                        BGUsers.MaxNickLength = int.Parse(theString);
                        Console.WriteLine("Maximum Nickname Length: " + BGUsers.MaxNickLength);
                        break;
                     case "passpattern":
                        BGUsers.PassPattern = Uri.UnescapeDataString(theString);
                        Console.WriteLine("Disallowed Password Chars: " + BGUsers.GetTestString(BGUsers.PassPattern));
                        break;
                     case "minpasslength":
                        BGUsers.MinPassLength = int.Parse(theString);
                        Console.WriteLine("Minimum Password Length: " + BGUsers.MinPassLength);
                        break;
                     case "maxpasslength":
                        BGUsers.MaxPassLength = int.Parse(theString);
                        Console.WriteLine("Maximum Password Length: " + BGUsers.MaxPassLength);
                        break;
                     case "avatartypes":
                        avatarTypes = theString.Split(';');
                        Console.WriteLine("Avatar File Types: " + string.Join(",", avatarTypes));
                        break;
                     case "modelfiles":
                        modelFiles = theString.Split(';');
                        Console.WriteLine("Model Files: " + string.Join(",", modelFiles));
                        break;
                     case "modelnames":
                        models = theString.Split(';');
                        Console.WriteLine("Model Names: " + string.Join(",", models));
                        break;
                     case "picsize":
                        string[] size = theString.Split('x');
                        if (size.Length == 2) {
                           try {
                              BGUsers.picSize = new System.Drawing.Size(int.Parse(size[0]), int.Parse(size[1]));
                              Console.WriteLine("Max Picture Size: " + theString);
                           } catch (FormatException) { }
                        }
                        break;
                  }
               }
            }
         }
         Console.WriteLine("---------------------------");
         theReader.Close();
      }




      /// <summary>
      /// Sets which files to use in case
      /// no file name is supplied, using
      /// the default file names
      /// </summary>
      internal void SetDefaultFileNames() {
         SetDefaultFileNames(DEFAULT_FILE_NAMES);
      }

      /// <summary>
      /// Sets which files to use in case
      /// no file name is supplied
      /// <param name="fileNames">Array of the various file names</param>
      /// </summary>
      internal void SetDefaultFileNames(string[] fileNames) {
         defaultFileNames = (string[])fileNames.Clone();
      }


      /// <summary>
      /// Uses the default directory as the
      /// working directory.
      /// </summary>
      internal void SetWorkingDirectory() {
         SetWorkingDirectory(DEFAULT_WORKING_DIR);
      }

      /// <summary>
      /// Set the directory to serve web
      /// pages from.
      /// </summary>
      /// <param name="dir">Directory pages are stored in.</param>
      internal void SetWorkingDirectory(string dir) {
         if (Directory.Exists(dir)) {
            workingDir = dir;
         } else if (Directory.Exists(DEFAULT_WORKING_DIR)) {
            Console.WriteLine("Working Directory not found, using default: " + DEFAULT_WORKING_DIR);
            workingDir = DEFAULT_WORKING_DIR;
         } else {
            Console.WriteLine("Working Directory not found. Creating it.");
            Directory.CreateDirectory(dir);
            if (Directory.Exists(dir)) {
               Console.WriteLine("Working Directory created.");
               workingDir = dir;
            }
         }
      }

      /// <summary>
      /// Initializes the WebServer
      /// </summary>
      internal static void Main() {
         Thread checkThread = new Thread(new ThreadStart(CheckConfigFile));
         checkThread.Start();
         server = new WebServer();
      }
   }

   /// <summary>
   /// Basic class that provides a lock object
   /// given a specific file name.
   /// <list type="bullet">
   /// <item>
   /// <term>Authors:</term>
   /// <description>Kenneth Beck</description>
   /// </item>
   /// </list>
   /// </summary>
   internal struct FileLocker {
      /// <summary>
      /// Dictionary object that contains a list of 
      /// current lockable items.
      /// </summary>
      private static Dictionary<string, object> theLocker = new Dictionary<string, object>();

      /// <summary>
      /// Gets a lock object based off the given string.
      /// </summary>
      /// <param name="lockOn">String used to get lock object</param>
      /// <returns>Lock object</returns>
      public static object GetLock(string lockOn) {
         if (!theLocker.ContainsKey(lockOn)) {
            theLocker.Add(lockOn, new object());
         }
         return theLocker[lockOn];
      }
   }
}