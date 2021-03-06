﻿using System.Collections.Generic;
using System.Text;
using Cave.IO;

namespace Cave.Net.Dns
{
    /// <summary>
    /// Provides a txt record.
    /// </summary>
    public class TxtRecord
    {
        /// <summary>Parses the record using the specified reader.</summary>
        /// <param name="reader">The reader.</param>
        /// <param name="length">The length.</param>
        /// <returns>Returns a new <see cref="TxtRecord"/> instance.</returns>
        public static TxtRecord Parse(DataReader reader, int length)
        {
            long end = reader.BaseStream.Position + length;
            var result = new TxtRecord();
            var parts = new List<string>();
            while (reader.BaseStream.Position < end)
            {
                int len = reader.ReadByte();
                parts.Add(reader.ReadString(len));
            }
            result.Parts = parts.ToArray();
            return result;
        }

        /// <summary>Gets the parts.</summary>
        /// <value>The parts.</value>
        public string[] Parts { get; private set; }

        /// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString()
        {
            var result = new StringBuilder();
            foreach (string part in Parts)
            {
                if (result.Length > 0)
                {
                    result.Append(' ');
                }

                if (part.IndexOfAny(new char[] { ' ', '"' }) > -1)
                {
                    result.Append('"');
                    result.Append(part.Replace("\"", "\"\""));
                    result.Append('"');
                }
                else
                {
                    result.Append(part);
                }
            }
            return result.ToString();
        }
    }
}
