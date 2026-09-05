using System;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Authentication
{
    public interface IUserService
    {
        User Add(string username, string password);
        User Update(User user);
        User Upsert(string username, string password);
        User FindUser();
        User FindUser(string username, string password);
        User FindUser(Guid identifier);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;

        public UserService(IUserRepository repo, IAppFolderInfo appFolderInfo, IDiskProvider diskProvider)
        {
            _repo = repo;
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
        }

        public User Add(string username, string password)
        {
            return _repo.Insert(new User
            {
                Identifier = Guid.NewGuid(),
                Username = username.ToLowerInvariant(),
                Password = PasswordHasher.Hash(password)
            });
        }

        public User Update(User user)
        {
            return _repo.Update(user);
        }

        public User Upsert(string username, string password)
        {
            var user = FindUser();

            if (user == null)
            {
                return Add(username, password);
            }

            if (password.IsNotNullOrWhiteSpace() && !PasswordHasher.Verify(password, user.Password))
            {
                user.Password = PasswordHasher.Hash(password);
            }
            else if (password.IsNotNullOrWhiteSpace() && PasswordHasher.NeedsRehash(user.Password))
            {
                user.Password = PasswordHasher.Hash(password);
            }

            user.Username = username.ToLowerInvariant();

            return Update(user);
        }

        public User FindUser()
        {
            return _repo.SingleOrDefault();
        }

        public User FindUser(string username, string password)
        {
            if (username.IsNullOrWhiteSpace() || password.IsNullOrWhiteSpace())
            {
                return null;
            }

            var user = _repo.FindUser(username.ToLowerInvariant());

            if (user == null)
            {
                return null;
            }

            if (!PasswordHasher.Verify(password, user.Password))
            {
                return null;
            }

            if (PasswordHasher.NeedsRehash(user.Password))
            {
                user.Password = PasswordHasher.Hash(password);
                _repo.Update(user);
            }

            return user;
        }

        public User FindUser(Guid identifier)
        {
            return _repo.FindUser(identifier);
        }
    }
}
