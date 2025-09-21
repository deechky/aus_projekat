using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read coil functions/requests.
    /// </summary>
    public class ReadCoilsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadCoilsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
		public ReadCoilsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc/>
        public override byte[] PackRequest()
        {
            //TO DO: IMPLEMENT
            // kreira niz bajtova za sve parametre za modbus
            var p = (ModbusReadCommandParameters)CommandParameters;
            byte[] request = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.TransactionId)), 0, request, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.ProtocolId)), 0, request, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Length)), 0, request, 4, 2);
            request[6] = p.UnitId;
            request[7] = p.FunctionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.StartAddress)), 0, request, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Quantity)), 0, request, 10, 2);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            //TO DO: IMPLEMENT
            // provjerava koji coil je ON a koji off
            var p = (ModbusReadCommandParameters)CommandParameters;
            var result = new Dictionary<Tuple<PointType, ushort>, ushort>();
            byte byteCount = response[8];
            ushort addr = p.StartAddress;

            for (int i = 0; i < byteCount; i++)
            {
                byte b = response[9 + i];
                for (int bit = 0; bit < 8 && addr < p.StartAddress + p.Quantity; bit++)
                {
                    ushort val = (ushort)((b >> bit) & 1);
                    result.Add(Tuple.Create(PointType.DIGITAL_OUTPUT, addr++), val);
                }
            }
            return result;
        }
    }
}