using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DkpDbContext))]
[Migration("20261004194638_AddSoftReservePurchases")]
partial class AddSoftReservePurchases
{
}
