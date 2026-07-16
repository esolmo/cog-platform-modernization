namespace AdminService.Entities;

public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public int AssignedByUserId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
