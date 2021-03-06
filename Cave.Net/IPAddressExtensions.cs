﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Cave.Net
{
    /// <summary>
    /// Extension class for the <see cref="IPAddress" /> class.
    /// </summary>
    public static class IPAddressExtensions
    {
        /// <summary>The ipv4 multicast address.</summary>
        public static readonly IPAddress IPv4MulticastAddress = IPAddress.Parse("224.0.0.0");

        /// <summary>The ipv6 multicast address.</summary>
        public static readonly IPAddress IPv6MulticastAddress = IPAddress.Parse("FF00::");

        /// <summary>
        /// Reverses the order of the bytes of an address.
        /// </summary>
        /// <param name="address"> Instance of the address, that should be reversed. </param>
        /// <returns> New reversed address. </returns>
        public static IPAddress Reverse(this IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            byte[] bytes = address.GetAddressBytes();
            Array.Reverse(bytes);
            return new IPAddress(bytes);
        }

        /// <summary>Returns a new address with the specified netmask.</summary>
        /// <param name="address">The address.</param>
        /// <param name="netmask">The netmask.</param>
        /// <returns>Returns an <see cref="IPAddress"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// address or netmask.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">AddressFamily of address and netmask do not match.</exception>
        public static IPAddress GetAddress(this IPAddress address, IPAddress netmask)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (netmask == null)
            {
                throw new ArgumentNullException(nameof(netmask));
            }

            if (address.AddressFamily != netmask.AddressFamily)
            {
                throw new ArgumentOutOfRangeException(nameof(netmask), "AddressFamily of address and netmask do not match");
            }

            byte[] result = address.GetAddressBytes();
            byte[] addr = address.GetAddressBytes();
            byte[] mask = netmask.GetAddressBytes();
            for (int i = 0; i < mask.Length; i++)
            {
                result[i] = (byte)(addr[i] & mask[i]);
            }
            return new IPAddress(result);
        }

        /// <summary>
        /// Gets the host ordered 32 bit integer representing the specified ip v4 address.
        /// </summary>
        /// <param name="address">An ip v4 address to convert.</param>
        /// <returns>Returns the host representation of the address.</returns>
        public static int ToInt32(this IPAddress address)
        {
            var bytes = address.GetAddressBytes();
            if (bytes.Length == 4)
            {
                return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0));
            }
#if NET20 || NET35
#else
            if (address.IsIPv4MappedToIPv6)
            {
                return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 12));
            }
#endif
            throw new NotSupportedException("IPv6 is not supported!");
        }

        /// <summary>
        /// Gets the local broadcast address for the specified <see cref="UnicastIPAddressInformation"/>.
        /// </summary>
        /// <param name="address">Address information.</param>
        /// <returns>Returns a local broadcast address.</returns>
        public static IPAddress ToBroadcast(this UnicastIPAddressInformation address)
        {
            if (address.Address.AddressFamily == AddressFamily.InterNetwork)
            {
                return ToBroadcast(address.Address, address.IPv4Mask);
            }
            throw new NotSupportedException($"AddressFamily {address.Address.AddressFamily} is not supported!");
        }

        /// <summary>
        /// Gets the local broadcast address for the specified <paramref name="address"/> and <paramref name="subnet"/> size combination.
        /// </summary>
        /// <param name="address">Address information.</param>
        /// <param name="subnet">The subnet size.</param>
        /// <returns>Returns a local broadcast address.</returns>
        public static IPAddress ToBroadcast(this IPAddress address, int subnet) => ToBroadcast(address, GetNetmask4(subnet));

        /// <summary>
        /// Gets the local  broadcast address for the specified <paramref name="address"/> and <paramref name="netmask"/> combination.
        /// </summary>
        /// <param name="address">Address information.</param>
        /// <param name="netmask">Netmask.</param>
        /// <returns>Returns a local broadcast address.</returns>
        public static IPAddress ToBroadcast(this IPAddress address, IPAddress netmask)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                int addr = ToInt32(address);
                int mask = ToInt32(netmask);
                int result = addr | ~mask;
                var data = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(result));
                return new IPAddress(data);
            }
            throw new NotSupportedException($"AddressFamily {address.AddressFamily} is not supported!");
        }

        /// <summary>Gets the netmask for IPv4.</summary>
        /// <param name="subnet">The subnet size.</param>
        /// <returns>Returns the specified ipv4 netmask.</returns>
        public static IPAddress GetNetmask4(int subnet)
        {
            byte[] data = new byte[4];
            for (int i = 0; i < 32; i++)
            {
                data[i / 8] = (byte)(1 << (7 - (i % 8)));
            }
            return new IPAddress(data);
        }

        /// <summary>Gets the netmask for IPv6.</summary>
        /// <param name="subnet">The subnet size.</param>
        /// <returns>Returns the specified ipv6 netmask.</returns>
        public static IPAddress GetNetmask6(int subnet)
        {
            byte[] data = new byte[16];
            for (int i = 0; i < 128; i++)
            {
                data[i / 8] = (byte)(1 << (7 - (i % 8)));
            }
            return new IPAddress(data);
        }

        /// <summary>Returns a new address with the specified netmask.</summary>
        /// <param name="address">The address.</param>
        /// <param name="netmask">The netmask.</param>
        /// <returns>Returns a new <see cref="IPAddress"/> instance.</returns>
        /// <exception cref="ArgumentNullException">address.</exception>
        /// <exception cref="ArgumentException">
        /// Netmask has to be in range of 0 to 32 on IPv4 addresses
        /// or
        /// Netmask has to be in range of 0 to 128 on IPv6 addresses.
        /// </exception>
        public static IPAddress GetAddress(this IPAddress address, int netmask)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                if ((netmask < 0) || (netmask > 32))
                {
                    throw new ArgumentException("Netmask have to be in range of 0 to 32 on IPv4 addresses", nameof(netmask));
                }

                return GetAddress(address, GetNetmask4(netmask));
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if ((netmask < 0) || (netmask > 128))
                {
                    throw new ArgumentException("Netmask have to be in range of 0 to 128 on IPv6 addresses", nameof(netmask));
                }

                return GetAddress(address, GetNetmask6(netmask));
            }
            throw new ArgumentOutOfRangeException(nameof(address), string.Format("Unknown ip address family {0}", address.AddressFamily));
        }

        /// <summary>
        /// Returns the reverse lookup address of an IPAddress.
        /// </summary>
        /// <param name="address"> Instance of the IPAddress, that should be used. </param>
        /// <returns> A string with the reverse lookup address. </returns>
        public static string GetReverseLookupAddress(this IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var res = new StringBuilder();
            byte[] bytes = address.GetAddressBytes();

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                for (int i = bytes.Length - 1; i >= 0; i--)
                {
                    res.Append(bytes[i]);
                    res.Append(".");
                }
                res.Append("in-addr.arpa");
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                for (int i = bytes.Length - 1; i >= 0; i--)
                {
                    string hex = bytes[i].ToString("x2");
                    res.Append(hex[1]);
                    res.Append(".");
                    res.Append(hex[0]);
                    res.Append(".");
                }
                res.Append("ip6.arpa");
            }
            else
            {
                throw new Exception("Invalid AddressFamily!");
            }

            return res.ToString();
        }

        /// <summary>
        /// Returns the reverse lookup DomainName of an IPAddress.
        /// </summary>
        /// <param name="address"> Instance of the IPAddress, that should be used. </param>
        /// <returns> A DomainName with the reverse lookup address. </returns>
        public static DomainName GetReverseLookupDomain(this IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var parts = new List<string>();
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                foreach (byte b in address.GetAddressBytes())
                {
                    parts.Add(b.ToString());
                }
                parts.Reverse();
                parts.Add("in-addr");
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                string s = address.GetAddressBytes().ToHexString();
                foreach (char c in s)
                {
                    parts.Add(c.ToString());
                }
                parts.Reverse();
                parts.Add("ip6");
            }
            else
            {
                throw new Exception("Invalid AddressFamily!");
            }

            parts.Add("arpa");
            return new DomainName(parts);
        }

        /// <summary>
        /// Returns a value indicating whether a ip address is a multicast address.
        /// </summary>
        /// <param name="address"> Instance of the IPAddress, that should be used. </param>
        /// <returns> true, if the given address is a multicast address; otherwise, false. </returns>
        public static bool IsMulticast(this IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return address.GetAddress(4).Equals(IPv4MulticastAddress);
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return address.GetAddress(8).Equals(IPv6MulticastAddress);
            }
            else
            {
                throw new Exception("Invalid AddressFamily!");
            }
        }

        /// <summary>Gets the broadcast address.</summary>
        /// <param name="unicastAddress">The unicast address.</param>
        /// <returns>Returns a new <see cref="IPAddress"/> instance.</returns>
        public static IPAddress GetBroadcastAddress(this UnicastIPAddressInformation unicastAddress)
        {
            uint ipAddress = BitConverter.ToUInt32(unicastAddress.Address.GetAddressBytes(), 0);
            uint ipMaskV4 = BitConverter.ToUInt32(unicastAddress.IPv4Mask.GetAddressBytes(), 0);
            uint broadCastIpAddress = ipAddress | ~ipMaskV4;
            return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
        }
    }
}
