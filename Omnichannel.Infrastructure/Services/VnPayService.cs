using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Infrastructure.Services
{
    public class VnPayService : IPaymentGatewayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(Order order, string ipAddress)
        {
            var tmnCode = _config["VnPayConfig:TmnCode"];
            var hashSecret = _config["VnPayConfig:HashSecret"];
            var baseUrl = _config["VnPayConfig:BaseUrl"];
            var returnUrl = _config["VnPayConfig:ReturnUrl"];

            var vnpayData = new SortedList<string, string>(new VnPayCompare())
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", tmnCode ?? "" },
                { "vnp_Amount", ((long)(order.TotalAmount * 100)).ToString() }, // VNPay requires amount * 100
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", ipAddress },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", $"Thanh toan don hang {order.OrderCode}" },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", returnUrl ?? "" },
                { "vnp_TxnRef", order.OrderCode }
            };

            var data = new StringBuilder();
            foreach (var kv in vnpayData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            var signData = data.ToString();
            if (signData.Length > 0)
            {
                signData = signData.Remove(signData.Length - 1, 1);
            }

            var vnpSecureHash = HmacSha512(hashSecret ?? "", signData);
            baseUrl += "?" + signData + "&vnp_SecureHash=" + vnpSecureHash;

            return baseUrl;
        }

        public Task<(bool Success, string OrderCode, decimal Amount)> ProcessCallbackAsync(IDictionary<string, string> queryData)
        {
            var hashSecret = _config["VnPayConfig:HashSecret"];
            var vnpayData = new SortedList<string, string>(new VnPayCompare());

            string vnp_SecureHash = "";
            foreach (var kv in queryData)
            {
                if (!string.IsNullOrEmpty(kv.Key) && kv.Key.StartsWith("vnp_"))
                {
                    if (kv.Key == "vnp_SecureHash")
                    {
                        vnp_SecureHash = kv.Value.ToString();
                    }
                    else if (kv.Key != "vnp_SecureHashType") // Exclude these
                    {
                        vnpayData.Add(kv.Key, kv.Value.ToString());
                    }
                }
            }

            var data = new StringBuilder();
            foreach (var kv in vnpayData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            var signData = data.ToString();
            if (signData.Length > 0)
            {
                signData = signData.Remove(signData.Length - 1, 1);
            }

            var checkSum = HmacSha512(hashSecret ?? "", signData);
            if (checkSum.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase))
            {
                if (queryData["vnp_ResponseCode"] == "00")
                {
                    string orderCode = queryData["vnp_TxnRef"].ToString();
                    if(decimal.TryParse(queryData["vnp_Amount"].ToString(), out decimal vnpAmount))
                    {
                        return Task.FromResult((true, orderCode, vnpAmount / 100m));
                    }
                }
            }

            return Task.FromResult((false, string.Empty, 0m));
        }

        private string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var Compare = string.CompareOrdinal(x, y);
            return Compare;
        }
    }
}
