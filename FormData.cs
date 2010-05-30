/*
 * Version:
 * $Id: FormData.cs,v 1.7 2007/02/03 22:16:18 kjb9089 Exp $
 * 
 * Revisions:
 * $Log: FormData.cs,v $
 * Revision 1.7  2007/02/03 22:16:18  kjb9089
 * - Corrected errors related to form input
 * - Added support for file upload and thus avatars
 * - Added more error codes
 *
 * Revision 1.6  2007/01/25 17:26:52  kjb9089
 * - Optimized code a bit (not perfectly, but better)
 * - Added admin rights (must edit .xml file to enable)
 * - Added more server-side parsing abilities
 *
 * Revision 1.5  2007/01/22 03:14:42  kjb9089
 * - Added index search feature (class[key]=value)
 *
 * Revision 1.4  2007/01/13 18:32:28  kjb9089
 * - Set AddValue() so that it adds empty values to existing keys
 *
 * Revision 1.3  2007/01/12 15:21:24  kjb9089
 * - Fixed AddValue() method so no error is thrown when the tableOfValues contains
 *   the specified index.
 *
 * Revision 1.2  2007/01/11 20:22:39  kjb9089
 * - Made change to AddValue() so that if a key exists, its value is replaced.
 *
 * Revision 1.1  2007/01/11 19:03:06  kjb9089
 * - Class file to assist the WebServer class
 *
 */

using System;
using System.Collections;

namespace BattleGrounds {
   /// <summary>
   /// A class that manages form information.
   /// MultiFormData inherits from this class.
   /// <list type="bullet">
   /// <item>
   /// <term>Authors:</term>
   /// <description>Kenneth Beck</description>
   /// </item>
   /// </list>
   /// </summary>
   public class FormData {
      protected Hashtable tableOfValues;
      protected string type;

      /// <summary>
      /// Initializes tableOfValues
      /// </summary>
      public FormData() {
         tableOfValues = new Hashtable();
      }

      /// <summary>
      /// Adds the name/value pair to the tableOfValues
      /// </summary>
      /// <param name="name">Index to use</param>
      /// <param name="value">Binary value to associate with index</param>
      public void AddValue(string name, byte[] value) {
         if (!tableOfValues.Contains(name)) {
            tableOfValues.Add(name, value);
         } else {
            tableOfValues[name] = value;
         }
      }

      /// <summary>
      /// Adds the name/value pair to the tableOfValues
      /// </summary>
      /// <param name="name">Index to use</param>
      /// <param name="value">Value to associate with index</param>
      public void AddValue(string name, string value) {
         value = Uri.UnescapeDataString(value.Replace('+', ' '));
         if (!tableOfValues.Contains(name)) {
            tableOfValues.Add(name, Uri.EscapeDataString(value));
         } else {
            tableOfValues[name] = Uri.EscapeDataString(value);
         }
      }

      /// <summary>
      /// Parses input (on the "=" sign) and then adds the name/value pair
      /// to the tableOfValues
      /// </summary>
      /// <param name="input">Input string to parse.</param>
      public void AddValue(string input) {
         string[] content = input.Split('=');
         if (content.Length == 2) {
            AddValue(content[0], content[1]);
         } else if (content.Length == 1) {
            AddValue(content[0], "");
         }
      }

      /// <summary>
      /// Returns the value at the requested index.
      /// </summary>
      /// <param name="name">Index to retrieve</param>
      /// <returns>Value associated with current index</returns>
      public string GetValue(string name) {
         if (tableOfValues.Contains(name)) {
            return "" + tableOfValues[name];
         }
         return "";
      }

      /// <summary>
      /// Property that eturns the length of the tableOfValues.
      /// </summary>
      /// <returns>Length of tableOfValues</returns>
      public int Length {
         get { return tableOfValues.Count; }
      }

      /// <summary>
      /// Property that returns the tableOfValues itself.
      /// </summary>
      /// <returns>The tableOfValues</returns>
      public Hashtable TableOfValues {
         get { return tableOfValues; }
      }

      /// <summary>
      /// Overrided ToString() method
      /// </summary>
      /// <returns>List of the name=value pairs in the tableOfValues</returns>
      public override string ToString() {
         string ret = "";
         foreach (string key in tableOfValues.Keys) {
            ret += key + "=" + tableOfValues[key] + "\r\n";
         }
         return ret;
      }

      /// <summary>
      /// Property that returns the type of the FormData object
      /// </summary>
      /// <returns>Type of the current FormData object</returns>
      public string Type {
         get { return type; }
      }

      public string this[string key] {
         get {
            return GetValue(key);
         }
         set {
            AddValue(key, value);
         }
      }
   }
}
