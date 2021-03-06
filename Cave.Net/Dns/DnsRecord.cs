﻿using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using Cave.IO;

namespace Cave.Net.Dns
{
    /// <summary>
    /// Provides a dns record.
    /// </summary>
    public class DnsRecord
    {
        /// <summary>Parses a record from the specified reader.</summary>
        /// <param name="reader">The reader.</param>
        /// <returns>Returns the dns record found.</returns>
        /// <exception cref="NotImplementedException">RecordType not implemented.</exception>
        public static DnsRecord Parse(DataReader reader)
        {
            long start = reader.BaseStream.Position;

            var result = new DnsRecord
            {
                Name = DomainName.Parse(reader),
                RecordType = (DnsRecordType)reader.ReadUInt16(),
                RecordClass = (DnsRecordClass)reader.ReadUInt16(),
                TimeToLive = reader.ReadInt32(),
            };

            int length = reader.ReadUInt16();
            if (length > reader.Available)
            {
                Trace.WriteLine("Additional data after dns answer.");
            }
            switch (result.RecordType)
            {
                case DnsRecordType.A:
                case DnsRecordType.AAAA: result.Value = new IPAddress(reader.ReadBytes(length)); break;
                case DnsRecordType.NS: result.Value = DomainName.Parse(reader); break;
                case DnsRecordType.SOA: result.Value = SoaRecord.Parse(reader); break;
                case DnsRecordType.MX: result.Value = MxRecord.Parse(reader); break;
                case DnsRecordType.TXT: result.Value = TxtRecord.Parse(reader, length); break;
                case DnsRecordType.CNAME: result.Value = DomainName.Parse(reader); break;
                default: throw new NotImplementedException($"RecordType {result.RecordType} not implemented!");
            }
            return result;
        }

        /// <summary>Gets the domain name.</summary>
        /// <value>The name.</value>
        public DomainName Name { get; private set; }

        /// <summary>Gets the type of the record.</summary>
        /// <value>The type of the record.</value>
        public DnsRecordType RecordType { get; private set; }

        /// <summary>Gets the record class.</summary>
        /// <value>The record class.</value>
        public DnsRecordClass RecordClass { get; private set; }

        /// <summary>Gets the time to live.</summary>
        /// <value>The time to live.</value>
        public int TimeToLive { get; private set; }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public object Value { get; private set; }

        /// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString()
        {
            var result = new StringBuilder();
            result.Append(Name);
            result.Append(" ");
            result.Append(RecordClass);
            result.Append(" ");
            result.Append(RecordType);
            if (Value != null)
            {
                result.Append(" ");
                result.Append(Value.ToString());
            }
            return result.ToString();
        }
    }
}
