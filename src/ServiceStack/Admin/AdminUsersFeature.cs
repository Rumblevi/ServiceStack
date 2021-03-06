using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.NativeTypes;
using ServiceStack.Text;

namespace ServiceStack.Admin
{
    public class AdminUsersFeature : IPlugin, Model.IHasStringId, IPostInitPlugin
    {
        public string Id { get; set; } = Plugins.AdminUsers;
        public string AdminRole { get; set; } = RoleNames.Admin;
        
        /// <summary>
        /// Remove UserAuth Properties from Admin Metadata
        /// </summary>
        public List<string> IncludeUserAuthProperties { get; set; } = new List<string> {
            nameof(UserAuth.Id),
            nameof(UserAuth.UserName),
            nameof(UserAuth.Email),
            nameof(UserAuth.DisplayName),
            nameof(UserAuth.FirstName),
            nameof(UserAuth.LastName),
            nameof(UserAuth.Company),
            nameof(UserAuth.Address),
            nameof(UserAuth.City),
            nameof(UserAuth.State),
            nameof(UserAuth.PostalCode),
            nameof(UserAuth.Country),
            nameof(UserAuth.PhoneNumber),
            nameof(UserAuth.LockedDate),
            nameof(IAuthSession.ProfileUrl),
        };

        /// <summary>
        /// Remove UserAuthDetails Properties from Admin Metadata
        /// </summary>
        public List<string> IncludeUserAuthDetailsProperties { get; set; } = new List<string>();

        /// <summary>
        /// Return only specified UserAuth Properties in AdminQueryUsers
        /// </summary>
        public List<string> QueryUserAuthProperties { get; set; } = new List<string> {
            nameof(UserAuth.Id),
            nameof(UserAuth.UserName),
            nameof(UserAuth.Email),
            nameof(UserAuth.DisplayName),
            nameof(UserAuth.FirstName),
            nameof(UserAuth.LastName),
            nameof(UserAuth.Company),
            nameof(UserAuth.State),
            nameof(UserAuth.Country),
            nameof(UserAuth.CreatedDate),
            nameof(UserAuth.ModifiedDate),
        };
        
        public void Register(IAppHost appHost)
        {
            appHost.RegisterService(typeof(AdminUsersService));
            
            appHost.AddToAppMetadata(meta => {
                var host = (ServiceStackHost) appHost;
                var authRepo = host.GetAuthRepository();
                if (authRepo == null)
                    return;
                
                using (authRepo as IDisposable)
                {
                    IUserAuth userAuth = new UserAuth();
                    IUserAuthDetails userAuthDetails = new UserAuthDetails();
                    if (authRepo is ICustomUserAuth customUserAuth)
                    {
                        userAuth = customUserAuth.CreateUserAuth();
                        userAuthDetails = customUserAuth.CreateUserAuthDetails();
                    }

                    var nativeTypesMeta = appHost.TryResolve<INativeTypesMetadata>() as NativeTypesMetadata 
                        ?? new NativeTypesMetadata(HostContext.AppHost.Metadata, new MetadataTypesConfig());
                    var metaGen = nativeTypesMeta.GetGenerator();

                    var plugin = meta.Plugins.AdminUsers = new AdminUsersInfo {
                        AccessRole = AdminRole,
                        Enabled = new List<string>(),
                        UserAuth = metaGen.ToType(userAuth.GetType()),
                        UserAuthDetails = metaGen.ToType(userAuthDetails.GetType()),
                        AllRoles = HostContext.Metadata.GetAllRoles(),
                        AllPermissions = HostContext.Metadata.GetAllPermissions(),
                        QueryUserAuthProperties = QueryUserAuthProperties,
                    };
                    if (authRepo is IQueryUserAuth)
                        plugin.Enabled.Add("query");
                    if (authRepo is ICustomUserAuth)
                        plugin.Enabled.Add("custom");
                    if (authRepo is IManageRoles)
                        plugin.Enabled.Add("manageRoles");

                    if (IncludeUserAuthProperties != null)
                    {
                        var map = plugin.UserAuth.Properties.ToDictionary(x => x.Name);
                        plugin.UserAuth.Properties = new List<MetadataPropertyType>();
                        foreach (var includeProp in IncludeUserAuthProperties)
                        {
                            if (map.TryGetValue(includeProp, out var prop))
                                plugin.UserAuth.Properties.Add(prop);
                        }
                    }
                    if (IncludeUserAuthDetailsProperties != null)
                    {
                        var map = plugin.UserAuthDetails.Properties.ToDictionary(x => x.Name);
                        plugin.UserAuthDetails.Properties = new List<MetadataPropertyType>();
                        foreach (var includeProp in IncludeUserAuthDetailsProperties)
                        {
                            if (map.TryGetValue(includeProp, out var prop))
                                plugin.UserAuthDetails.Properties.Add(prop);
                        }
                    }
                }
            });
        }

        public void AfterPluginsLoaded(IAppHost appHost)
        {
            var authRepo = ((ServiceStackHost)appHost).GetAuthRepositoryAsync();
            if (authRepo == null)
                throw new Exception("UserAuth Repository is required to use " + nameof(AdminUsersFeature));
        }
    }

    public abstract class AdminUserBase : IMeta
    {
        [DataMember(Order = 1)] public string UserName { get; set; }
        [DataMember(Order = 2)] public string FirstName { get; set; }
        [DataMember(Order = 3)] public string LastName { get; set; }
        [DataMember(Order = 4)] public string DisplayName { get; set; }
        [DataMember(Order = 5)] public string Email { get; set; }
        [DataMember(Order = 6)] public string Password { get; set; }
        [DataMember(Order = 7)] public string ProfileUrl { get; set; }
        [DataMember(Order = 8)] public Dictionary<string, string> UserAuthProperties { get; set; }
        [DataMember(Order = 9)] public Dictionary<string, string> Meta { get; set; }
    }
    
/* Allow metadata discovery & code-gen in *.Source.csproj builds */    
#if !SOURCE
    [ExcludeMetadata] public partial class AdminCreateUser {}
    [ExcludeMetadata] public partial class AdminUpdateUser {}
    [ExcludeMetadata] public partial class AdminGetUser {}
    [ExcludeMetadata] public partial class AdminDeleteUser {}
    [ExcludeMetadata] public partial class AdminQueryUsers {}
    
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public partial class AdminUsersService {}
#endif
    
    [DataContract]
    public partial class AdminCreateUser : AdminUserBase, IPost, IReturn<AdminUserResponse>
    {
        [DataMember(Order = 10)] public List<string> Roles { get; set; }
        [DataMember(Order = 11)] public List<string> Permissions { get; set; }
    }
    
    [DataContract]
    public partial class AdminUpdateUser : AdminUserBase, IPut, IReturn<AdminUserResponse>
    {
        [DataMember(Order = 10)] public string Id { get; set; }
        [DataMember(Order = 11)] public bool? LockUser { get; set; }
        [DataMember(Order = 12)] public bool? UnlockUser { get; set; }
        [DataMember(Order = 13)] public List<string> AddRoles { get; set; }
        [DataMember(Order = 14)] public List<string> RemoveRoles { get; set; }
        [DataMember(Order = 15)] public List<string> AddPermissions { get; set; }
        [DataMember(Order = 16)] public List<string> RemovePermissions { get; set; }
    }
    
    [DataContract]
    public partial class AdminGetUser : IGet, IReturn<AdminUserResponse>
    {
        [DataMember(Order = 10)] public string Id { get; set; }
    }
    
    [DataContract]
    public partial class AdminDeleteUser : IDelete, IReturn<AdminDeleteUserResponse>
    {
        [DataMember(Order = 10)] public string Id { get; set; }
    }

    [DataContract]
    public class AdminDeleteUserResponse : IHasResponseStatus
    {
        [DataMember(Order = 1)] public string Id { get; set; }
        [DataMember(Order = 2)] public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public partial class AdminUserResponse : IHasResponseStatus
    {
        [DataMember(Order = 1)] public string Id { get; set; }
        [DataMember(Order = 2)] public Dictionary<string,object> Result { get; set; }
        [DataMember(Order = 3)] public ResponseStatus ResponseStatus { get; set; }
    }
    
    [DataContract]
    public partial class AdminQueryUsers : IGet, IReturn<AdminUsersResponse>
    {
        [DataMember(Order = 1)] public string Query { get; set; }
        [DataMember(Order = 2)] public string OrderBy { get; set; }
        [DataMember(Order = 3)] public int? Skip { get; set; }
        [DataMember(Order = 4)] public int? Take { get; set; }
    }

    [DataContract]
    public class AdminUsersResponse : IHasResponseStatus
    {
        [DataMember(Order = 1)] public List<Dictionary<string,object>> Results { get; set; }
        [DataMember(Order = 2)] public ResponseStatus ResponseStatus { get; set; }
    }

    public partial class AdminUsersService : Service
    {
        public static ValidateFn ValidateFn { get; set; }

        private async Task<object> Validate(AdminUserBase request)
        {
            await RequiredRoleAttribute.AssertRequiredRoleAsync(
                Request, AssertPlugin<AdminUsersFeature>().AdminRole);
            
            var authFeature = GetPlugin<AuthFeature>();
            if (authFeature != null)
            {
                if (authFeature.SaveUserNamesInLowerCase)
                {
                    if (request.UserName != null)
                        request.UserName = request.UserName.ToLower();
                    if (request.Email != null)
                        request.Email = request.Email.ToLower();
                }
            }
                        
            var validateResponse = ValidateFn?.Invoke(this, HttpMethods.Post, request);
            return validateResponse;
        }

        public async Task<object> Get(AdminGetUser request)
        {
            if (request.Id == null)
                throw new ArgumentNullException(nameof(request.Id));
            
            var existingUser = await AuthRepositoryAsync.GetUserAuthAsync(request.Id);
            return await CreateUserResponse(existingUser);
        }

        public async Task<object> Get(AdminQueryUsers request)
        {
            // Do exact search by Username/Email if Auth Repo doesn't support querying
            if (!(AuthRepositoryAsync is IQueryUserAuthAsync) && !(AuthRepository is IQueryUserAuth))
            {
                var user = await AuthRepositoryAsync.GetUserAuthByUserNameAsync(request.Query);
                return new AdminUsersResponse {
                    Results = new List<Dictionary<string, object>> { ToUserProps(user) }
                };
            }
            
            var users = !string.IsNullOrEmpty(request.Query)
                ? await AuthRepositoryAsync.SearchUserAuthsAsync(request.Query, request.OrderBy, request.Skip, request.Take)
                : await AuthRepositoryAsync.GetUserAuthsAsync(request.OrderBy, request.Skip, request.Take);

            var feature = AssertPlugin<AdminUsersFeature>();
            var userResults = FilterResults(users.Map(ToUserProps), feature.QueryUserAuthProperties);
            return new AdminUsersResponse {
                Results = userResults,
            };
        }

        private List<Dictionary<string, object>> FilterResults(List<Dictionary<string, object>> results, List<string> includeProps)
        {
            if (includeProps == null)
                return results;

            var to = new List<Dictionary<string, object>>();

            foreach (var result in results)
            {
                var row = new Dictionary<string, object>();
                foreach (var includeProp in includeProps)
                {
                    row[includeProp] = result.TryGetValue(includeProp, out var value)
                        ? value
                        : null;
                }
                to.Add(row);
            }
            
            return to;
        }
        
        public async Task<object> Post(AdminCreateUser request)
        {
            var validateResponse = await Validate(request);
            if (validateResponse != null)
                return validateResponse;

            if (await AuthRepositoryAsync.GetUserAuthByUserNameAsync(request.UserName).ConfigAwait() != null)
                throw HttpError.Validation("AlreadyExists", ErrorMessages.UsernameAlreadyExists.Localize(base.Request), nameof(request.UserName));
            if (await AuthRepositoryAsync.GetUserAuthByUserNameAsync(request.Email).ConfigAwait() != null)
                throw HttpError.Validation("AlreadyExists", ErrorMessages.EmailAlreadyExists.Localize(base.Request), nameof(request.Email));

            var newUser = PopulateUserAuth(AuthRepositoryAsync is ICustomUserAuth customUserAuth ? customUserAuth.CreateUserAuth() : new UserAuth(), request);
            IUserAuth user = await AuthRepositoryAsync.CreateUserAuthAsync(newUser, request.Password).ConfigAwait();
            if (!request.Roles.IsEmpty() || !request.Roles.IsEmpty())
            {
                await AuthRepositoryAsync.AssignRolesAsync(user, request.Roles, request.Permissions);
            }

            return await CreateUserResponse(user);
        }

        public async Task<object> Put(AdminUpdateUser request)
        {
            if (request.Id == null)
                throw new ArgumentNullException(nameof(request.Id));
            
            var validateResponse = await Validate(request);
            if (validateResponse != null)
                return validateResponse;

            var existingUser = await AuthRepositoryAsync.GetUserAuthAsync(request.Id);
            if (existingUser == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(Request));
            
            var newUser = PopulateUserAuth(existingUser, request);

            if (request.LockUser == true)
            {
                newUser.LockedDate = DateTime.UtcNow;
            }
            if (request.UnlockUser == true)
            {
                newUser.LockedDate = null;
                newUser.InvalidLoginAttempts = 0;
            }
            
            if (!string.IsNullOrEmpty(request.Password))
                existingUser = await AuthRepositoryAsync.UpdateUserAuthAsync(existingUser, newUser, request.Password);
            else
                existingUser = await AuthRepositoryAsync.UpdateUserAuthAsync(existingUser, newUser);

            if (!request.AddRoles.IsEmpty() || !request.AddPermissions.IsEmpty())
                await AuthRepositoryAsync.AssignRolesAsync(existingUser, request.AddRoles, request.AddPermissions);
            if (!request.RemoveRoles.IsEmpty() || !request.RemovePermissions.IsEmpty())
                await AuthRepositoryAsync.UnAssignRolesAsync(existingUser, request.RemoveRoles, request.RemovePermissions);

            return await CreateUserResponse(existingUser);
        }
        
        public async Task<object> Delete(AdminDeleteUser request)
        {
            if (request.Id == null)
                throw new ArgumentNullException(nameof(request.Id));
            
            await AuthRepositoryAsync.DeleteUserAuthAsync(request.Id);
            return new AdminDeleteUserResponse {
                Id = request.Id,
            };
        }

        private async Task<object> CreateUserResponse(IUserAuth user)
        {
            if (user == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(Request));
            
            var userProps = await GetUserPropsAndRoles(user);

            return new AdminUserResponse {
                Id = user.Id.ToString(),
                Result = userProps
            };
        }

        private async Task<Dictionary<string, object>> GetUserPropsAndRoles(IUserAuth user)
        {
            if (AuthRepositoryAsync is IManageRolesAsync manageRoles)
            {
                var tuple = await manageRoles.GetRolesAndPermissionsAsync( user.Id.ToString());
                user.Roles = tuple.Item1.ToList();
                user.Permissions = tuple.Item2.ToList();
            }

            return ToUserProps(user);
        }

        private static Dictionary<string, object> ToUserProps(IUserAuth user)
        {
            var userProps = user.ToObjectDictionary();
            userProps.Remove(nameof(IUserAuth.PasswordHash));
            userProps.Remove(nameof(IUserAuth.Salt));

            if (userProps.TryGetValue(nameof(IUserAuth.Meta), out var meta) && meta is Dictionary<string,string> metaMap)
            {
                if (metaMap.TryGetValue(nameof(IAuthSession.ProfileUrl), out var profileUrl))
                    userProps[nameof(IAuthSession.ProfileUrl)] = profileUrl;
            }

            return userProps;
        }

        public IUserAuth PopulateUserAuth(IUserAuth to, AdminUserBase request)
        {
            to.PopulateWithNonDefaultValues(request);
            if (!string.IsNullOrEmpty(request.Email))
                to.PrimaryEmail = request.Email;

            if (to.DisplayName == null && to.FirstName != null)
                to.DisplayName = to.FirstName + (to.LastName != null ? " " + to.LastName : "");

            request.UserAuthProperties.PopulateInstance(request);

            var hasProfileUrlProp = TypeProperties.Get(to.GetType()).PropertyMap.ContainsKey(nameof(IAuthSession.ProfileUrl));
            if (request.ProfileUrl != null && !hasProfileUrlProp)
            {
                to.Meta ??= new Dictionary<string, string>();
                to.Meta[nameof(IAuthSession.ProfileUrl)] = request.ProfileUrl;
            }
            
            return to;
        }
        
    }
}