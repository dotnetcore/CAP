namespace SkyWalking.Diagnostics.EntityFrameworkCore.Tests.Fakes
{
    public class FakeUser
    {
        public FakeUser()
        {
        }

        public FakeUser(string firstName, string lastName)
        {
        }

        public string Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public override string ToString()
        {
            return $"Output for [User] - Id:{Id}, FirstName:{FirstName}, LastName:{LastName}";
        }
    }
}
