using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read holding registers functions/requests.
    /// </summary>
    public class ReadHoldingRegistersFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadHoldingRegistersFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadHoldingRegistersFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            ModbusCommandParameters paramCom = this.CommandParameters as ModbusCommandParameters;
            byte[] request = new byte[12];
            Buffer.BlockCopy((Array)BitConverter.GetBytes(
                    IPAddress.HostToNetworkOrder(
                        (short)paramCom.TransactionId)),
                0,
                (Array)request,
                0,
                2);
            Buffer.BlockCopy((Array)BitConverter.GetBytes(
                    IPAddress.HostToNetworkOrder(
                        (short)paramCom.ProtocolId)),
                0,
                (Array)request,
                2,
                2);
            Buffer.BlockCopy((Array)BitConverter.GetBytes(
                    IPAddress.HostToNetworkOrder(
                        (short)paramCom.Length)),
                0,
                (Array)request,
                4,
                2);
            request[6] = paramCom.UnitId;
            request[7] = paramCom.FunctionCode;
            Buffer.BlockCopy((Array)BitConverter.GetBytes(
                    IPAddress.HostToNetworkOrder(
                        (short)(paramCom as ModbusReadCommandParameters).StartAddress)),
                0,
                (Array)request,
                8,
                2);
            Buffer.BlockCopy((Array)BitConverter.GetBytes(
                    IPAddress.HostToNetworkOrder(
                        (short)(paramCom as ModbusReadCommandParameters).Quantity)),
                0,
                (Array)request,
                10,
                2);
            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            ModbusReadCommandParameters paramCom = this.CommandParameters as ModbusReadCommandParameters;
            Dictionary<Tuple<PointType, ushort>, ushort> d = new Dictionary<Tuple<PointType, ushort>, ushort>();

            ushort startAddress = paramCom.StartAddress;
            byte byteCount = response[8];
            ushort value;

            for (int i = 0; i < byteCount; i += 2)
            {
                Tuple<PointType, ushort> address = new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, startAddress++);
                value = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, 9 + i));
                d.Add(address, value);
            }
            return d;
        }
    }
}