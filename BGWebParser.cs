/*
 * $Id: BGWebParser.cs,v 1.1 2007/02/22 02:32:34 kjb9089 Exp $
 * 
 * $Log: BGWebParser.cs,v $
 * Revision 1.1  2007/02/22 02:32:34  kjb9089
 * - Final Version before final submission
 *
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BattleGrounds {
   internal class BGWebParser {
      /// <summary>
      /// Parses a .bgw file
      /// </summary>
      /// <param name="theInfo">Contains HeaderInfo for the user</param>
      /// <returns>Parsed .bgw file</returns>
      internal static byte[] ParseFileContents(HeaderInfo theInfo) {
         string includesPat = @"%INCLUDEFILE<file>(.+)</file>%/INCLUDEFILE(\r\n)?";
         string returnMsg = "";
         if (theInfo.FullPath.Equals(WebServer.workingDir+"about//")) {
            returnMsg = @"<html>
<head>
<title>About " + WebServer.serverName + @"</title>
<link rel=""stylesheet"" href=""/BGStyle.css"">
</head>
<body>
%INCLUDEFILE<file>menu.bgw</file>%/INCLUDEFILE
<table align=""center"" width=""500"">
   <tr>
      <td align=""center""><i>Information as of: </i><u>" + DateTime.Now + @"</u><i> (server time)</i><br></td>
   </tr>
   <tr>
      <td>Server Uptime: <b>" + (DateTime.Now - WebServer.startTime) + @"</b></td>
   </tr>
   <tr>
      <td>Current # Of Users: <b>" + BGUsers.GetUsers().Length + @"</b></td>
   </tr>
   <tr>
      <td>Total Connections Made: <b>" + WebConnection.ttlConnections + @"</b></td>
   </tr>
   <tr>
      <td align=""center""><hr><i>This web server was designed and written by </i><b>Kenneth Beck</b><i>, for a 
         Freshman Computer Science class at Rochester Institute of Technology.
         It was written as a tool for a game designed for the same class, a game
         called BattleGrounds. BattleGrounds was written and designed by </i><b>Travis 
         Popkave</b>, <b>Steve Willard</b>, <i>and</i> <b>Kevin Hockey</b>. <i>People who create an account 
         via this web site can also log onto the game with the same account information.</i></b></td>
   </tr>
</table>
</body>
</head>";
         } else {
            returnMsg = Encoding.ASCII.GetString(theInfo.returnData);
         }
         MatchCollection includes = Regex.Matches(returnMsg, includesPat);
         if (includes.Count > 0) {
            for (int i = 0; i < includes.Count; i++) {
               returnMsg = Regex.Replace(returnMsg, includes[i].Groups[0].Value,
                  Encoding.ASCII.GetString(WebConnection.GetFileContents(WebServer.workingDir +
                  "/" + includes[i].Groups[1].Value)) + "\r\n");
            }
         }

         if (BGUsers.CheckLogged(theInfo)) {
            returnMsg = Regex.Replace(returnMsg, @"%LOGGEDOUT((\r\n)*.*?)+%/LOGGEDOUT(\r\n)?", "");
            returnMsg = Regex.Replace(returnMsg, @"%LOGGEDIN(\r\n)?", "");
            returnMsg = Regex.Replace(returnMsg, @"%/LOGGEDIN(\r\n)?", "");
            returnMsg = Regex.Replace(returnMsg, @"\$USER_AVATAR", "<img src=\"/avatars/" +
               theInfo.cookieData["avatar"] + "\">");
         } else {
            returnMsg = Regex.Replace(returnMsg, @"%LOGGEDIN((\r\n)*.*?)+%/LOGGEDIN(\r\n)?", "");
            returnMsg = Regex.Replace(returnMsg, @"%LOGGEDOUT(\r\n)?", "");
            returnMsg = Regex.Replace(returnMsg, @"%/LOGGEDOUT(\r\n)?", "");
         }

         if (BGUsers.CheckLogged(theInfo) && theInfo.cookieData != null &&
             theInfo.cookieData["rights"].Contains("admin")) {
            returnMsg = Regex.Replace(returnMsg, @"%ADMIN(\r\n)?", "");
            returnMsg = Regex.Replace(returnMsg, @"%/ADMIN(\r\n)?", "");
            if (returnMsg.Contains("$ADMIN_USERS")) {
               string users = @"
<table align=""center"" border=""1"" cellpadding=""0"" cellspacing=""0"" >
   <tr>
      <td colspan=""5"" align=""center""><b><i>$FORM_MSG</i></b></td>
   </tr>
   <tr>
      <td align=""center"" colspan=""5""><b>Current Users<b></td>
      <td rowspan=""2"" colspan=""2"">&nbsp;</td>
   </tr>
   <tr>
      <td align=""center""><u>Add Time:</u></td>
      <td align=""center""><u>Full Name</u></td>
      <td align=""center""><u>Username</u></td>
      <td align=""center""><u>Info</u></td>
      <td align=""center""><u>Avatar</u></td>
   </tr>";
               string color = "000000";
               foreach (string file in BGUsers.GetUsers()) {
                  string name = Regex.Match(file, @".*?/(.*?)\.xml").Groups[1].Value;
                  if (name != "") {
                     FormData user = BGUsers.GetUserData(name);
                     if (color == "000000") {
                        color = "555555";
                     } else {
                        color = "000000";
                     }
                     users += "\r\n   <tr bgcolor=\"" + color + "\">\r\n" +
                        "      <td align=\"center\">&nbsp;" + Uri.UnescapeDataString(user["addtime"]) + "</td>\r\n" +
                        "      <td align=\"center\">&nbsp;" + Uri.UnescapeDataString(user["fullname"]) + "</td>\r\n" +
                        "      <td align=\"center\">&nbsp;" + name + "</td>\r\n" +
                        "      <td align=\"center\">&nbsp;" + Uri.UnescapeDataString(user["info"]) + "</td>\r\n" +
                        "      <td align=\"center\">&nbsp;<img src=\"avatars/" + user["avatar"] + "\"></td>\r\n" +
                        "      <td align=\"center\">";
                     if (user["nick"] == theInfo.cookieData["nick"]) {
                        users += "<i>Self</i>";
                     } else if (user["rights"].Contains("superadmin")) {
                        users += "<i>Super</i>";
                     } else if ((!user["rights"].Contains("admin") ||
                         theInfo.cookieData["rights"].Contains("superadmin")) &&
                         !name.Equals(theInfo.cookieData["nick"])) {
                        users += "<a href=\"viewUsers.bgw?action=delUser&nick=" + name + "\">Delete</a>";

                     } else {
                        users += "<i>Admin</i>";
                     }
                     users += "</td>\r\n" +
                     "      <td align=\"center\">";
                     if (user["rights"].Contains("superadmin")) {
                        users += "&nbsp;";
                     } if (!user["rights"].Contains("admin")) {
                        users += "+<a href=\"viewUsers.bgw?action=makeAdmin&nick=" + name + "\">Admin</a>";
                     } else if (theInfo.cookieData["rights"].Contains("superadmin") &&
                         user["nick"] != theInfo.cookieData["nick"]) {
                        users += "-<a href=\"viewUsers.bgw?action=revokeAdmin&nick=" + name + "\">Admin</a>";
                     } else {
                        users += "&nbsp;";
                     }
                     users += "</td>\r\n" +
                        "   </tr>\r\n";
                  }
               }
               users += "</table>";
               returnMsg = Regex.Replace(returnMsg, @"\$ADMIN_USERS", users);
            }
         } else {
            returnMsg = Regex.Replace(returnMsg, @"%ADMIN((\r\n)*.*?)+%/ADMIN(\r\n)?", "");
         }


         if (theInfo.cookieData != null) {
            foreach (string name in theInfo.cookieData.TableOfValues.Keys) {
               if (name != "") {
                  string theTest = "$COOKIE_" + name.ToUpper();
                  if (returnMsg.Contains(theTest)) {
                     returnMsg = returnMsg.Replace(theTest, Uri.UnescapeDataString(theInfo.cookieData[name]));
                  }
               }
            }
         }

         if (theInfo.parsedData != null) {
            foreach (string name in theInfo.parsedData.TableOfValues.Keys) {
               if (name != "") {
                  string theTest = "$FORM_" + name.ToUpper();
                  if (returnMsg.Contains(theTest)) {
                     returnMsg = Regex.Replace(returnMsg, @"\$CONTAINS_" + name.ToUpper(), "");
                     returnMsg = Regex.Replace(returnMsg, @"\$/CONTAINS_" + name.ToUpper(), "");
                     returnMsg = returnMsg.Replace(theTest, Uri.UnescapeDataString(theInfo.parsedData[name]));
                  } else {
                     returnMsg = Regex.Replace(returnMsg, @"\$CONTAINS_" +
                        name.ToUpper() + @"((\r\n)*.*?)+\$/CONTAINS_" + name.ToUpper(), "");
                  }
               }
            }
         }

         string addSelect = "\r\n<select name=\"model\">";
         string editSelect = addSelect;
         for (int i = 0; i < WebServer.modelFiles.Length; i++) {
            addSelect += "\r\n   <option value=\"" + WebServer.modelFiles[i] + "\">" + WebServer.models[i];
            editSelect += "\r\n   <option value=\"" + WebServer.modelFiles[i] + "\"" + (theInfo.cookieData != null &&
               theInfo.cookieData["model"].Equals(WebServer.modelFiles[i]) ? " selected" : "") + ">" + WebServer.models[i];
         }
         addSelect += "\r\n</select>";
         editSelect += "\r\n</select>";
         returnMsg = Regex.Replace(returnMsg, @"\$ADD_MODEL", addSelect);
         returnMsg = Regex.Replace(returnMsg, @"\$EDIT_MODEL", editSelect);

         returnMsg = Regex.Replace(returnMsg, "#SERVER_AVATAR_TYPES", string.Join(", ", WebServer.avatarTypes));
         returnMsg = Regex.Replace(returnMsg, "#SERVER_MAX_SIZE",
            WebServer.maxFileUploadSize / 1024 + "kb; " +
            BGUsers.picSize.Width + "x" + BGUsers.picSize.Height);

         returnMsg = Regex.Replace(returnMsg, @"\$/?[A-Z_]+", "");

         theInfo.returnData = Encoding.ASCII.GetBytes(returnMsg);

         return Encoding.ASCII.GetBytes(returnMsg);
      }
   }
}