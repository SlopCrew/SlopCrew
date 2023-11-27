using Microsoft.EntityFrameworkCore;

namespace SlopCrew.Server.Database;

public class CrewService(SlopDbContext dbContext) {
    public const int MaxCrewCount = 10;

    public Task<List<Crew>> GetCrews(User user) {
        var crews = dbContext.Crews.Where(c => c.Members.Contains(user));
        return crews.ToListAsync();
    }

    public Task<Crew?> GetCrew(string id)
        => dbContext.Crews.FirstOrDefaultAsync(c => c.Id == id);

    public bool CanJoinOrCreateCrew(User user)
        => user.Crews.Count < MaxCrewCount;

    public bool CanLeaveCrew(Crew crew, User user) {
        // Wasn't in it in the first place
        if (!crew.Members.Contains(user)) return false; // not in it in the first place

        // Must have at least one owner at all times
        if (crew.Owners.Contains(user) && crew.Owners.Count == 1) return false;

        // Ditto for members
        if (crew.Members.Contains(user) && crew.Members.Count == 1) return false;

        return true;
    }

    public async Task<Crew> CreateCrew(User user, string name, string tag) {
        if (!this.CanJoinOrCreateCrew(user)) {
            throw new InvalidOperationException("User cannot create any more crews");
        }

        var crew = new Crew {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Tag = tag,
            Owners = [user],
            Members = [user]
        };

        await dbContext.Crews.AddAsync(crew);
        user.Crews.Add(crew);
        user.OwnedCrews.Add(crew);
        await dbContext.SaveChangesAsync();

        return crew;
    }

    public async Task<bool> JoinCrew(User user, string inviteCode) {
        if (!this.CanJoinOrCreateCrew(user)) return false;

        var crewWithCode = await dbContext.Crews.FirstOrDefaultAsync(c => c.InviteCodes.Contains(inviteCode));
        if (crewWithCode is null) return false;

        crewWithCode.Members.Add(user);
        user.Crews.Add(crewWithCode);
        crewWithCode.InviteCodes.Remove(inviteCode);
        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task LeaveCrew(Crew crew, User user) {
        if (!this.CanLeaveCrew(crew, user)) {
            throw new InvalidOperationException("User cannot leave crew");
        }

        if (crew.Owners.Contains(user)) crew.Owners.Remove(user);
        if (crew.Members.Contains(user)) crew.Members.Remove(user);

        if (user.OwnedCrews.Contains(crew)) user.OwnedCrews.Remove(crew);
        if (user.Crews.Contains(crew)) user.Crews.Remove(crew);

        if (user.RepresentingCrew == crew) {
            user.RepresentingCrew = null;
            user.RepresentingCrewId = null;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteCrew(Crew crew) {
        foreach (var member in crew.Members) {
            var user = await dbContext.Users
                           .Include(user => user.RepresentingCrew)
                           .Include(user => user.Crews)
                           .Include(user => user.OwnedCrews)
                           .FirstOrDefaultAsync(u => u == member);
            if (user is null) continue;

            if (user.Crews.Contains(crew)) user.Crews.Remove(crew);
            if (user.OwnedCrews.Contains(crew)) user.OwnedCrews.Remove(crew);
            if (user.RepresentingCrew == crew) {
                user.RepresentingCrew = null;
                user.RepresentingCrewId = null;
            }
        }

        dbContext.Crews.Remove(crew);
        await dbContext.SaveChangesAsync();
    }

    public async Task<string> GenerateInviteCode(Crew crew) {
        var code = Guid.NewGuid().ToString();
        crew.InviteCodes.Add(code);
        await dbContext.SaveChangesAsync();
        return code;
    }

    public async Task DeleteInviteCode(Crew crew, string code) {
        crew.InviteCodes.Remove(code);
        await dbContext.SaveChangesAsync();
    }

    public async Task PromoteUser(Crew crew, User user) {
        if (crew.Owners.Contains(user)) return; // Already done
        crew.Owners.Add(user);
        user.OwnedCrews.Add(crew);
        await dbContext.SaveChangesAsync();
    }

    public async Task DemoteUser(Crew crew, User user) {
        if (crew.Owners.Count == 1) return;      // Must have at least one owner
        if (!crew.Owners.Contains(user)) return; // Already done

        crew.Owners.Remove(user);
        user.OwnedCrews.Remove(crew);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateCrew(Crew crew, string newName, string newTag) {
        crew.Name = newName;
        crew.Tag = newTag;
        await dbContext.SaveChangesAsync();
    }
}
