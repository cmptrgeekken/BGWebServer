/*
 * Version:
 * $Id: MultiFormData.cs,v 1.5 2007/02/22 02:32:07 kjb9089 Exp $
 * 
 * Revisions:
 * $Log: MultiFormData.cs,v $
 * Revision 1.5  2007/02/22 02:32:07  kjb9089
 * - Final Version before final submission
 *
 * Revision 1.4  2007/02/06 18:08:15  kjb9089
 * - Fixed file upload-related issues
 *
 * Revision 1.3  2007/02/04 20:19:43  kjb9089
 * - Refined how multiform data is parsed, more efficient now
 *
 * Revision 1.2  2007/02/03 22:16:36  kjb9089
 * - Corrected errors related to form input
 * - Added support for file upload and thus avatars
 * - Added more error codes
 *
 * Revision 1.1  2007/01/11 19:03:18  kjb9089
 * - Class file to assist the WebServer class
 *
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BattleGrounds {
   /// <summary>
   /// A class for managing the data contained within
   /// a multipart form.
   /// <list type="bullet">
   /// <item>
   /// <term>Authors:</term>
   /// <description>Kenneth Beck</description>
   /// </item>
   /// </list>
   /// </summary>
   internal class MultiFormData : FormData {
      internal Hashtable tableOfInfo;

      /// <summary>
      /// Sets the type and initializes the Hashtable.
      /// </summary>
      internal MultiFormData()
         : base() {
         type = "multi";
         tableOfInfo = new Hashtable();
      }

      /// <summary>
      /// Parses supplied multipart data and adds it to
      /// the tableOfInfo and tableOfValues.
      /// </summary>
      /// <param name="theContent">Multipart data to parse.</param>
      internal void AddContent(string theContent, List<byte> recMsg) {
         MultiInfo theInfo = new MultiInfo();
         string msg = Encoding.ASCII.GetString(recMsg.ToArray());
         string name = "", value = "";

         value = theContent.Substring(theContent.IndexOf("\r\n\r\n") + 4);

         theContent = theContent.Substring(0, theContent.IndexOf("\r\n\r\n"));
         theInfo.disposition = Regex.Match(theContent, "Content-Disposition: (.*?);").Groups[1].Value;
         name = Regex.Match(theContent, "name=\"(.*?)\"").Groups[1].Value;
         theInfo.filename = Regex.Match(theContent, "filename=\"(.*?)\"").Groups[1].Value;
         if (theInfo.filename != "") {
            theInfo.type = Regex.Match(theContent, "Content-Type: (.*?)$").Groups[1].Value;
            theInfo.contents = new byte[value.Length];
            recMsg.CopyTo(msg.IndexOf(value), theInfo.contents, 0, value.Length);
            AddValue(name, theInfo.filename);

            if (!tableOfInfo.ContainsKey(name)) {
               tableOfInfo.Add(name, theInfo);
            }
         } else {
            AddValue(name, value);
         }
      }
   }

   /// <summary>
   /// A basic struct containing three fields that
   /// a multipart form would contain
   /// <list type="bullet">
   /// <item>
   /// <term>Authors:</term>
   /// <description>Kenneth Beck</description>
   /// </item>
   /// </list>
   /// </summary>
   internal struct MultiInfo {
      internal string disposition, filename, type;
      internal byte[] contents;
   }
}