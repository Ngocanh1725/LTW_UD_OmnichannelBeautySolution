using System;
using Microsoft.AspNetCore.Identity;

class Program {
    static void Main() {
        var h = new PasswordHasher<string>();
        Console.WriteLine("ADMIN_HASH=" + h.HashPassword("OmnichannelSystemUser", "Admin@123456"));
        Console.WriteLine("MANAGER_HASH=" + h.HashPassword("OmnichannelSystemUser", "Manager@123456"));
        Console.WriteLine("CASHIER_HASH=" + h.HashPassword("OmnichannelSystemUser", "Cashier@123456"));
    }
}
