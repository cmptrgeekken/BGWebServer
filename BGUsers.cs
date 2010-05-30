/*
 * Version:
 * $Id: BGUsers.cs,v 1.17 2007/02/22 02:31:58 kjb9089 Exp $
 * 
 * Revisions:
 * $Log: BGUsers.cs,v $
 * Revision 1.17  2007/02/22 02:31:58  kjb9089
 * - Final Version before final submission
 *
 * Revision 1.16  2007/02/07 04:26:22  kjb9089
 * - Fixed as many fricken errors as I could. Soooo frustrating.
 *
 * Revision 1.15  2007/02/06 18:08:07  kjb9089
 * - Fixed file upload-related issues
 *
 * Revision 1.14  2007/02/05 05:54:40  kjb9089
 * - Lots and lots of growling.
 *
 * Revision 1.13  2007/02/05 05:05:56  kjb9089
 * - Fixed errors involved with logging in and with profile updates.
 *
 * Revision 1.12  2007/02/04 20:18:46  kjb9089
 * - Added commenting
 * - Refined some of the code, still have more refining to do
 * - Added avatar file saving capabilities
 *
 * Revision 1.11  2007/02/03 22:16:14  kjb9089
 * - Corrected errors related to form input
 * - Added support for file upload and thus avatars
 * - Added more error codes
 *
 * Revision 1.10  2007/01/25 17:26:49  kjb9089
 * - Optimized code a bit (not perfectly, but better)
 * - Added admin rights (must edit .xml file to enable)
 * - Added more server-side parsing abilities
 *
 * Revision 1.9  2007/01/22 18:43:31  kjb9089
 * - Added too and fixed cookie features
 *
 * Revision 1.8  2007/01/22 03:15:24  kjb9089
 * - Added EditUser, a cookie parser, and WriteUserDoc function
 *
 * Revision 1.7  2007/01/15 22:55:20  kjb9089
 * - Cleaned up code for 2nd Deadline
 *
 * Revision 1.6  2007/01/13 18:30:05  kjb9089
 * - Added checks for nickname length & fullname length
 *
 * Revision 1.5  2007/01/12 18:19:55  kjb9089
 * - Added error page support
 *
 * Revision 1.4  2007/01/12 15:22:41  kjb9089
 * - Added password pattern checking
 *
 * Revision 1.3  2007/01/11 20:23:01  kjb9089
 * - Expanded Input Check
 *
 * Revision 1.2  2007/01/11 19:57:03  kjb9089
 * - Added input checker for name, nick, and password. Does not provide
 *   an error page for incorrect input as of yet.
 *
 * Revision 1.1  2007/01/11 19:03:02  kjb9089
 * - Class file to assist the WebServer class
 *
 */

using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace BattleGrounds {
   /// <summary>
   /// A class designed to hold and maintainheader information
   /// <list type="bullet">
   /// <item>
   /// <term>Authors:</term>
   /// <description>Kenneth Beck</description>
   /// </item>
   /// </list>
   /// </summary>
   internal class BGUsers {
      private const string DEFAULT_FULLNAME_PATTERN = @"\w\s";
      private const string DEFAULT_NICK_PATTERN = @"\w";
      private const string DEFAULT_PASS_PATTERN = @"^\s\\\/\<\>";
      private const int DEFAULT_MIN_FULLNAME_LENGTH = 0;
      private const int DEFAULT_MAX_FULLNAME_LENGTH = 25;
      private const int DEFAULT_MIN_NICK_LENGTH = 3;
      private const int DEFAULT_MAX_NICK_LENGTH = 15;
      private const int DEFAULT_MIN_PASS_LENGTH = 8;
      private const int DEFAULT_MAX_PASS_LENGTH = 24;
      private const int DEFAULT_KEY_LENGTH = 20;
      private const int DEFAULT_PIC_WIDTH = 32;
      private const int DEFAULT_PIC_HEIGHT = 32;

      private static string fullNamePattern = DEFAULT_FULLNAME_PATTERN;
      internal static string FullNamePattern { get { return fullNamePattern; } set { fullNamePattern = value; } }
      private static int minFullNameLength = DEFAULT_MIN_FULLNAME_LENGTH;
      internal static int MinFullNameLength { get { return minFullNameLength; } set { minFullNameLength = value; } }
      private static int maxFullNameLength = DEFAULT_MAX_FULLNAME_LENGTH;
      internal static int MaxFullNameLength { get { return maxFullNameLength; } set { maxFullNameLength = value; } }

      private static string nickPattern = DEFAULT_NICK_PATTERN;
      internal static string NickPattern { get { return nickPattern; } set { nickPattern = value; } }
      private static int minNickLength = DEFAULT_MIN_NICK_LENGTH;
      internal static int MinNickLength { get { return minNickLength; } set { minNickLength = value; } }
      private static int maxNickLength = DEFAULT_MAX_NICK_LENGTH;
      internal static int MaxNickLength { get { return maxNickLength; } set { maxNickLength = value; } }


      private static string passPattern = DEFAULT_PASS_PATTERN;
      internal static string PassPattern { get { return passPattern; } set { passPattern = value; } }
      private static int minPassLength = DEFAULT_MIN_PASS_LENGTH;
      internal static int MinPassLength { get { return minPassLength; } set { minPassLength = value; } }
      private static int maxPassLength = DEFAULT_MAX_PASS_LENGTH;
      internal static int MaxPassLength { get { return maxPassLength; } set { maxPassLength = value; } }

      internal static Size picSize = new Size(DEFAULT_PIC_WIDTH, DEFAULT_PIC_HEIGHT);
      private static string[] fields = new string[] {
         "fullname",
         "nick",
         "password",
         "info",
         "key",
         "rights",
         "avatar",
         "addtime",
         "model"
      };

      /// <summary>
      /// A namespace-accessible method for
      /// adding a user.
      /// </summary>
      /// <param name="theData">Form information (should contain user data)</param>
      internal static FormData AddUser(FormData theData) {
         string nick = theData["nick"];
         if (File.Exists(GetUserFile(nick)))
            throw new BGException("475", new string[] { nick });

         if (nick.Length < minNickLength)
            throw new BGException("477", new string[] { "Nickname", "" + minNickLength });

         if (nick.Length > maxNickLength)
            throw new BGException("478", new string[] { "Nickname", "" + maxNickLength });

         if (GetNotAllowed(nick, nickPattern) != "")
            throw new BGException("476", new string[] { "Nickname", GetNotAllowed(nick, nickPattern) });
         if (!Directory.Exists(WebServer.UserDir)) {
            Directory.CreateDirectory(WebServer.UserDir);
         }

         if (theData["password"] == "" || theData["passCheck"] == "")
            throw new BGException("473", new string[] { });


         theData["addtime"] = DateTime.Now.ToString();

         WriteUserDoc(theData);
         theData.AddValue("MSG", "User <b>'" + Uri.UnescapeDataString(nick +
            "'</b> added successfully.<br>\r\n"));
         return theData;
      }

      /// <summary>
      /// Checks whether the user is logged in.
      /// Bases this information off user's username
      /// and session hash.
      /// </summary>
      /// <param name="theInfo">Contains user's information.</param>
      /// <returns>True if logged in. False otherwise.</returns>
      internal static bool CheckLogged(HeaderInfo theInfo) {
         FormData tmpData = GetCookieInfo(theInfo["Cookie"]);
         tmpData = GetUserData(tmpData["nick"]);
         if (tmpData != null && tmpData["key"] != "" &&
             theInfo.cookieData != null &&
             theInfo.cookieData["key"] == tmpData["key"]) {
            theInfo.isLoggedIn = true;
            theInfo.cookieData = tmpData;
            return true;
         }
         theInfo.isLoggedIn = false;
         return false;
      }

      /// <summary>
      /// Updates the logged-in user's profile.
      /// </summary>
      /// <param name="theInfo">HeaderInfo containing proper information</param>
      internal static void EditUser(HeaderInfo theInfo) {
         string nick = theInfo.cookieData["nick"];
         string hash = theInfo.cookieData["key"];
         if (nick != "" && hash != "") {
            FormData oldData = GetUserData(nick);
            if (oldData != null && oldData["key"] == hash) {
               theInfo.parsedData["nick"] = nick;
               WriteUserDoc(theInfo.parsedData);
               theInfo.parsedData["MSG"] = "<b>User Edited Successfully.</b><br>\r\n";
            }
         }
      }

      /// <summary>
      /// Parses cookie info
      /// </summary>
      /// <param name="cookieString">Cookie string to parse</param>
      /// <returns>FormData containing cookie info</returns>
      internal static FormData GetCookieInfo(string cookieString) {
         FormData data = new FormData();
         GetCookieInfo(cookieString, data);
         return data;
      }

      /// <summary>
      /// Parses cookie information.
      /// </summary>
      /// <param name="cookieString">Cookie string to parse.</param>
      /// <param name="theData">FormData to add cookie information to.</param>
      internal static void GetCookieInfo(string cookieString, FormData theData) {
         MatchCollection cookies = Regex.Matches(cookieString,
            @"([a-z]+)=([" + Regex.Replace(BGUsers.NickPattern, @"\^", "") + "]+)");

         if (theData == null) {
            theData = new FormData();
         }

         foreach (Match cookie in cookies) {
            if (cookie.Groups[1].Value == "username") {
               theData.AddValue("nick", cookie.Groups[2].Value);
            } else if (cookie.Groups[1].Value == "hash") {
               theData.AddValue("key", cookie.Groups[2].Value);
            }
         }
      }

      /// <summary>
      /// Deletes the given user's user file.
      /// </summary>
      /// <param name="userName">User to delete</param>
      /// <returns>True if successful. False otherwise.</returns>
      internal static bool DeleteUser(string userName) {
         FormData userData = GetUserData(userName);
         if (File.Exists(GetUserFile(userName))) {
            lock (FileLocker.GetLock(GetUserFile(userName))) {
               File.Delete(GetUserFile(userName));
            }
            if (File.Exists(GetAvatarFile(userName))) {
               lock (FileLocker.GetLock(GetAvatarFile(userName))) {
                  File.Delete(GetAvatarFile(userName));
               }
            }
            return true;
         }
         return false;
      }

      /// <summary>
      /// Checks to see if the user is logged in.
      /// </summary>
      /// <param name="theInfo">Info to base this off of</param>
      internal static void HandleCookie(HeaderInfo theInfo) {
         FormData tmp = GetCookieInfo(theInfo["Cookie"]);
         FormData saved = GetUserData(tmp["nick"]);
         if (saved != null && tmp["key"] == saved["key"]) {
            theInfo.cookieData = saved;
            theInfo.isLoggedIn = true;
         } else {
            theInfo.isLoggedIn = false;
         }
      }

      /// <summary>
      /// Checks to see if the file has the proper file extension
      /// for file upload.
      /// </summary>
      /// <param name="file">File to check</param>
      /// <returns>True if file has a proper extension. False otherwise</returns>
      private static bool HasProperExtension(string file) {
         string ext = file.Substring(file.LastIndexOf(".") + 1);
         for (int i = 0; i < WebServer.AvatarTypes.Length; i++) {
            if (ext.ToLower() == WebServer.AvatarTypes[i]) {
               return true;
            }
         }
         return false;
      }

      /// <summary>
      /// Used to determine if the requested field
      /// is allowed in the user data file.
      /// </summary>
      /// <param name="field">Requested field</param>
      /// <returns>True if its an allowed field, false otherwise</returns>
      private static bool isAField(string field) {
         for (int i = 0; i < fields.Length; i++) {
            if (field == fields[i]) {
               return true;
            }
         }
         return false;
      }

      /// <summary>
      /// Used when displaying what is allowed for a given pattern.
      /// </summary>
      /// <param name="pattern">Given pattern</param>
      /// <returns>String containing all accepted characters</returns>
      internal static string GetTestString(string pattern) {
         string test1 = "A";
         string test2 = "a";
         string test3 = "0";
         string test4 = " ";
         string test5 = "!@#$%^&*()_+-=[]{}|;':,./<>?";
         string match = "";

         pattern = "[" + pattern + "]";

         if (test1.Equals(Regex.Replace(test1, pattern, "")))
            match += "(A-Z), ";
         if (test2.Equals(Regex.Replace(test2, pattern, "")))
            match += "(a-z), ";
         if (test3.Equals(Regex.Replace(test3, pattern, "")))
            match += "(0-9), ";
         if (test4.Equals(Regex.Replace(test4, pattern, "")))
            match += "spaces, ";
         if (!Regex.Replace(test5, pattern, "").Equals(""))
            match += "( " + Regex.Replace(test5, pattern, "") + " )";
         return match;
      }

      /// <summary>
      /// Returns the contents of the string
      /// that are not allowed based on the given pattern.
      /// </summary>
      /// <param name="before">String to check</param>
      /// <param name="pattern">Pattern to use</param>
      /// <returns>contents of the string that are not allowed</returns>
      private static string GetNotAllowed(string before, string pattern) {
         string notAllowed = Regex.Replace(before, "[" + pattern + "]", "");
         string ret = "";
         foreach (char a in notAllowed) {
            if (ret.IndexOf(a) == -1) {
               ret += a;
            }
         }
         ret = Regex.Replace(ret, "(.)", " ($0), ");
         ret = Regex.Replace(ret, @"\(\s\)", "spaces");
         return ret;
      }

      /// <summary>
      /// Removes all characters that are not allowed
      /// from the string.
      /// </summary>
      /// <param name="before">String to strip</param>
      /// <returns>Stripped string</returns>
      private static string RemoveNotAllowed(string before, string pattern) {
         return Uri.EscapeDataString(Regex.Replace(Uri.UnescapeDataString(before),
            "[" + pattern + "]", ""));
      }

      /// <summary>
      /// Method for encoding passwords
      /// </summary>
      /// <param name="password">Password string</param>
      /// <returns>Encoded password</returns>
      private static string EncodePassword(string password) {
         return password;
      }

      /// <summary>
      /// Returns a data string containing the location to the 
      /// avatar file for the supplied nickname.
      /// </summary>
      /// <param name="nick"></param>
      /// <returns></returns>
      private static string GetAvatarFile(string nick) {
         return WebServer.userDir + "avatars/" + GetUserData(nick)["avatar"];
      }

      /// <summary>
      /// Returns a random collection of characters
      /// of length DEFAULT_KEY_LENGTH
      /// </summary>
      private static string GetUserKey() {
         Random rand = new Random();
         string keyString = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
         string key = "";
         for (int i = 0; i < DEFAULT_KEY_LENGTH; i++) {
            key += keyString[rand.Next(keyString.Length)];
         }
         return key;
      }

      /// <summary>
      /// Returns a list of all users currently in the database.
      /// </summary>
      internal static string[] GetUsers() {
         string[] users = new string[0];
         if (Directory.Exists(WebServer.UserDir)) {
            users = Directory.GetFiles(WebServer.UserDir);
         }
         return users;
      }

      /// <summary>
      /// Places user information into a FormData variable.
      /// </summary>
      /// <param name="userName">User name to look up</param>
      /// <returns>Data for current user or null if user not found</returns>
      internal static FormData GetUserData(string userName) {
         string userFile = GetUserFile(userName);
         FormData theData = new FormData();
         if (!File.Exists(userFile)) {
            return theData;
         }
         lock (FileLocker.GetLock(userFile)) {
            XmlTextReader theReader = new XmlTextReader(userFile);

            theReader.MoveToContent();

            while (theReader.Read()) {
               if (theReader.NodeType == XmlNodeType.Element) {
                  theData.AddValue(theReader.Name, theReader.ReadString());
               }
            }
            theReader.Close();
            theReader = null;
         }
         return theData;
      }

      /// <summary>
      /// Returns the user file string
      /// for the requested user.
      /// </summary>
      /// <param name="userName">Requested user</param>
      /// <returns>File name for user.</returns>
      private static string GetUserFile(string userName) {
         return WebServer.UserDir + userName + ".xml";
      }

      /// <summary>
      /// Namespace-accessible method for logging in a user.
      /// </summary>
      /// <param name="theData">Form information (should contain necessary user data)</param>
      internal static void LogInUser(HeaderInfo theInfo) {
         string username, password;
         if (!CheckLogged(theInfo)) {
            if ((username = theInfo.parsedData["username"]) != "") {
               if ((password = theInfo.parsedData["password"]) != "") {
                  FormData tmpData = GetUserData(username);
                  if (tmpData.Length > 0 && tmpData["password"] == EncodePassword(password)) {
                     string key = GetUserKey();
                     tmpData["key"] = key;
                     tmpData["password"] = EncodePassword(tmpData["password"]);
                     WriteUserDoc(tmpData);
                     theInfo["Cookie"] = "username=" + tmpData["nick"] + "; hash=" + key;
                     theInfo["Set-Cookie"] = "username=" + tmpData["nick"] + "|hash=" + key;
                     theInfo.isLoggedIn = true;
                     HandleCookie(theInfo);
                  } else {
                     throw new BGException("479", new string[] { username });
                  }
               } else {
                  throw new BGException("473", new string[0]);
               }
            } else {
               throw new BGException("470", new string[0]);
            }
         } else {
            throw new BGException("480", new string[0]);
         }
      }

      /// <summary>
      /// Grants or revokes admin priviliges
      /// for the supplied nickname.
      /// </summary>
      /// <param name="nick">Nickname to grant/revoke privileges</param>
      /// <param name="isAdmin">True: Grant privileges, False: Revoke privileges</param>
      internal static void MakeAdmin(string nick, bool isAdmin) {
         FormData userData = GetUserData(nick);
         userData["rights"] = (isAdmin ? "admin" : "");
         WriteUserDoc(userData);
      }


      /// <summary>
      /// Method used to write an avatar to file for the supplied username.
      /// </summary>
      /// <param name="nick">Nickname to write avatar for</param>
      /// <param name="avatar">Name of the avatar</param>
      /// <param name="contents">Byte array containing file contents</param>
      private static void WriteImageFile(string nick, string avatar, byte[] contents) {
         string fileName = WebServer.UserDir + "avatars/" + Uri.UnescapeDataString(avatar);
         Image image;
         string tmpFile = fileName + ".tmp";

         lock (FileLocker.GetLock(tmpFile)) {
            FileStream stream = new FileStream(tmpFile, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(contents);
            writer.Close();
            stream.Close();
            image = Bitmap.FromFile(tmpFile);

            if (image.Width > picSize.Width || image.Height > picSize.Height) {
               image.Dispose();
               File.Delete(tmpFile);
               throw new BGException("452", new string[] { picSize.Width + " x " + picSize.Height });
            } else {
               image.Dispose();
               if (File.Exists(GetAvatarFile(nick))) {
                  lock (FileLocker.GetLock(GetAvatarFile(nick))) {
                     File.Delete(GetAvatarFile(nick));
                  }
               }
               if (File.Exists(fileName)) {
                  lock (FileLocker.GetLock(fileName)) {
                     File.Delete(fileName);
                  }
               }
               File.Copy(fileName + ".tmp", fileName);
               File.Delete(tmpFile);
            }
         }
      }

      /// <summary>
      /// Writes the user documentation using the provided formdata
      /// </summary>
      /// <param name="theData">Data to write.</param>
      private static void WriteUserDoc(FormData theData) {
         string fullname = Uri.UnescapeDataString(theData["fullname"]);
         string nick = Uri.UnescapeDataString(theData["nick"]);
         string info = theData["info"];
         string password = Uri.UnescapeDataString(theData["password"]);
         string passCheck = Uri.UnescapeDataString(theData["passCheck"]);
         string fileName = GetUserFile(nick);
         bool checkPass = false;

         if (theData["avatar"] != "" &&
            theData.GetType() == typeof(MultiFormData) &&
            ((MultiFormData)theData).tableOfInfo.Count > 0) {
            if (HasProperExtension(theData["avatar"])) {
               byte[] file = ((MultiInfo)(((MultiFormData)theData).tableOfInfo["avatar"])).contents;
               if (file.Length > 0) {
                  theData["avatar"] = theData["nick"] +
                     theData["avatar"].Substring(theData["avatar"].LastIndexOf('.')).ToLower();
                  WriteImageFile(theData["nick"], theData["avatar"], file);
               } else {
                  theData["avatar"] = GetUserData(nick)["avatar"];
               }
            } else {
               throw new BGException("471", new string[] { string.Join(", ", WebServer.AvatarTypes) });
            }
         } else {
            theData["avatar"] = GetUserData(nick)["avatar"];
         }

         if (theData["action"] == "updateUser") {
            FormData oldData = GetUserData(nick);
            if (theData["oldPass"] != "") {
               if (oldData["password"] == EncodePassword(theData["oldPass"])) {
                  if (theData["password"] == "" && theData["passCheck"] == "") {
                     password = theData["oldPass"];
                     passCheck = theData["oldPass"];
                  }
                  checkPass = true;
               } else {
                  throw new BGException("472", new string[] { });
               }
            } else {
               theData["password"] = oldData["password"];
            }
            foreach (string key in oldData.TableOfValues.Keys) {
               if (!theData.TableOfValues.Contains(key)) {
                  theData[key] = oldData[key];
               }
            }
         }

         if (theData["action"] == "adduser" || checkPass) {
            if (!password.Equals(passCheck))
               throw new BGException("474", new string[] { });

            if (GetNotAllowed(password, passPattern) != "")
               throw new BGException("476", new string[] { "Password", 
                  GetNotAllowed(password, passPattern) });

            if ((password.Length < minPassLength &&
                !theData["rights"].Contains("superadmin")) ||
                password.Length == 0)
               throw new BGException("477", new string[] { "Password", "" + minPassLength });

            if (password.Length > maxPassLength)
               throw new BGException("478", new string[] { "Password", "" + maxPassLength });

            theData["password"] = EncodePassword(password);
         }

         if (GetNotAllowed(fullname, fullNamePattern) != "")
            throw new BGException("476", new string[] { "Full name", 
               GetNotAllowed(fullname, fullNamePattern) });

         if (fullname.Length < minFullNameLength)
            throw new BGException("477", new string[] { "Full name", "" + minFullNameLength });
         if (fullname.Length > maxFullNameLength)
            throw new BGException("478", new string[] { "Full name", "" + maxFullNameLength });

         lock (FileLocker.GetLock(GetUserFile(nick))) {
            XmlTextWriter theWriter = new XmlTextWriter(GetUserFile(theData["nick"]), null);

            theWriter.Formatting = Formatting.Indented;

            theWriter.WriteStartDocument();
            theWriter.WriteStartElement("user");

            foreach (DictionaryEntry entry in theData.TableOfValues) {
               if (isAField(entry.Key.ToString())) {
                  theWriter.WriteStartElement("" + entry.Key);
                  theWriter.WriteValue(entry.Value);
                  theWriter.WriteEndElement();
               }
            }

            theWriter.WriteEndElement();
            theWriter.WriteEndDocument();
            theWriter.Close();
         }
      }
   }
}