/*
 * Version:
 * $Id: POSTData.cs,v 1.9 2007/02/22 02:32:12 kjb9089 Exp $
 * 
 * Revisions:
 * $Log: POSTData.cs,v $
 * Revision 1.9  2007/02/22 02:32:12  kjb9089
 * - Final Version before final submission
 *
 * Revision 1.8  2007/02/04 20:20:34  kjb9089
 * - Changed how multiform data is parsed, utilizing Regex
 *
 * Revision 1.7  2007/02/03 22:16:40  kjb9089
 * - Corrected errors related to form input
 * - Added support for file upload and thus avatars
 * - Added more error codes
 *
 * Revision 1.6  2007/02/02 15:36:13  kjb9089
 * - Start of adding better multipart forms (will lead to the ability to upload files)
 *
 * Revision 1.5  2007/01/25 17:27:03  kjb9089
 * - Optimized code a bit (not perfectly, but better)
 * - Added admin rights (must edit .xml file to enable)
 * - Added more server-side parsing abilities
 *
 * Revision 1.4  2007/01/15 22:59:26  kjb9089
 * - Cleaned up code for 2nd Deadline
 *
 * Revision 1.3  2007/01/13 18:37:04  kjb9089
 * - Attempted to add Opera support
 *
 * Revision 1.2  2007/01/12 15:23:54  kjb9089
 * - Made POSTData work with a referenced FormData variable, so that keep-alive
 *   connections can be properly handled
 *
 * Revision 1.1  2007/01/11 19:03:23  kjb9089
 * - Class file to assist the WebServer class
 *
 */

using System;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BattleGrounds {
   /// <summary>
   /// A class designed to handle POST messages 
   /// received from a client.
   /// <list type="bullet">
   /// <item>
   /// <term>Authors:</term>
   /// <description>Kenneth Beck</description>
   /// </item>
   /// </list>
   /// </summary>
   class POSTData {

      /// <summary>
      /// Method that handles POST data.
      /// Can split:
      ///  * multipart/form-data
      ///  * application/x-www-form-urlencoded
      /// enctypes
      /// </summary>
      /// <param name="theInfo">Variable to place the header info into</param>
      internal static void HandlePostMsg(HeaderInfo theInfo) {
         string formData = Encoding.ASCII.GetString(theInfo.formData);
         if (formData.Length > 0) {
            if (theInfo["Content-Type"].Contains("multipart/form-data")) {
               string[] multiData = new string[0];
               string pattern = "((--)?" + theInfo["boundary"] + 
                  "(--)?\r\n)?(.*?)\r\n(--)?" + theInfo["boundary"] + "(--)?";
               MatchCollection matches = Regex.Matches(formData, pattern, 
                  RegexOptions.Singleline);
               if (matches.Count > 0) {
                  int i = 0;
                  multiData = new string[matches.Count];
                  foreach (Match match in matches) {
                     multiData[i] = match.Groups[4].Value;
                     if (multiData[i][0] == '\r') {
                        multiData[i] = multiData[i].Substring(2);
                     }
                     i++;
                  }
               }

               theInfo.parsedData = new MultiFormData();
               for (int i = 0; i < multiData.Length; i++) {
                  if (multiData[i] != "") {
                     ((MultiFormData)theInfo.parsedData).AddContent(multiData[i], theInfo.tmpRecMsg);
                  }
               }
            } else if (theInfo["Content-Type"].Contains("application/x-www-form-urlencoded")) {
               if (theInfo.parsedData == null) {
                  theInfo.parsedData = new FormData();
               }

               for (int i = 0; i < formData.Split('&').Length; i++) {
                  string[] vals = formData.Split('&')[i].Split('=');
                  if (vals.Length == 2) {
                     theInfo.parsedData.AddValue(vals[0], vals[1]);
                  }
               }
            }
         }
      }
   }
}