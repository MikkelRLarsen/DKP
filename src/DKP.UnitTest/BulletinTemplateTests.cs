using DKP.Domain;
using Xunit;

namespace DKP.UnitTest;

public sealed class BulletinTemplateTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Template_validates_and_can_be_deactivated_and_reactivated()
    {
        var template = new BulletinTemplate("Dungeon", "Complete dungeon", "Run the dungeon", 25, 2, true, TimeSpan.FromDays(1), Now);

        Assert.True(template.IsActive);
        Assert.Equal(TimeSpan.FromDays(1), template.DefaultDeadlineOffset);

        template.SetActive(false, Now.AddMinutes(1));
        Assert.False(template.IsActive);
        template.SetActive(true, Now.AddMinutes(2));
        Assert.True(template.IsActive);
    }

    [Fact]
    public void Template_rejects_invalid_values()
    {
        Assert.Throws<ArgumentException>(() => new BulletinTemplate("", "Title", "Description", 1, 1, false, null, Now));
        Assert.Throws<ArgumentException>(() => new BulletinTemplate("Name", "Title", "Description", 0, 1, false, null, Now));
        Assert.Throws<ArgumentException>(() => new BulletinTemplate("Name", "Title", "Description", 1, 1, false, TimeSpan.Zero, Now));
    }
}
