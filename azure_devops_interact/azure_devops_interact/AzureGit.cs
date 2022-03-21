using azure_devops_interact.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azure_devops_interact
{
    public class AzureGit
    {
        public VssConnection Connection { get; }

        private readonly GitHttpClient gitClient;
        public GitContext Context { get; private set; }

        public AzureGit(VssConnection connection)
        {
            Connection = connection;
            gitClient = connection.GetClient<GitHttpClient>();
            Context = new GitContext();
        }

        public async Task<GitRepository> GetRepo()
        {

            GitRepository repo = await gitClient.GetRepositoryAsync(Context.ProjectName, Context.RepoName);

            return repo;
        }

        public async Task<bool> CommitFiles(GitRef branch, string commitMessage, List<Tuple<string, string>> files)
        {
            var changes = new List<GitChange>();

            foreach (var file in files)
            {
                var change = new GitChange()
                {
                    ChangeType = VersionControlChangeType.Add,
                    Item = new GitItem() { Path = file.Item1 },
                    NewContent = new ItemContent()
                    {
                        Content = file.Item2,
                        ContentType = ItemContentType.RawText,
                    },
                };
                changes.Add(change);
            }

            GitCommitRef newCommit = new GitCommitRef()
            {
                Comment = commitMessage,
                Changes = changes
            };

            GitRepository repo = await GetRepo();

            // create the push with the new branch and commit
            GitPush push = gitClient.CreatePushAsync(new GitPush()
            {
                RefUpdates = new GitRefUpdate[] { new GitRefUpdate { OldObjectId = branch.ObjectId, Name = branch.Name } },
                Commits = new GitCommitRef[] { newCommit },
            }, repo.Id).Result;

            return true;
        }


        public async Task<GitRefUpdateResult> CreateBranch(string newBranchName)
        {

            GitRepository repo = await GetRepo();

            string defaultBranch = repo.DefaultBranch.Replace("refs/", "");
            GitRef sourceRef = gitClient.GetRefsAsync(repo.Id, filter: defaultBranch).Result.First();

            // create a new branch from the source
            GitRefUpdateResult refCreateResult = gitClient.UpdateRefsAsync(
                new GitRefUpdate[] { new GitRefUpdate() {
                    OldObjectId = new string('0', 40),
                    NewObjectId = sourceRef.ObjectId,
                    Name = $"refs/heads/{newBranchName}",
                } },
                repositoryId: repo.Id).Result.First();

            return refCreateResult;
        }



        public async Task<GitRef> GetBranch(string branchName)
        {

            GitRepository repo = await GetRepo();

            List<GitRef> sourceRefs = await gitClient.GetRefsAsync(repo.Id, filter: $"heads/{branchName}");

            return sourceRefs.FirstOrDefault();
        }




        public async Task<List<GitRef>> GetAllBranches()
        {

            GitRepository repo = await GetRepo();

            var sourceRefs = await gitClient.GetBranchRefsAsync(repo.Id);

            return sourceRefs;
        }

        public async Task<List<GitItem>> AllItems(List<GitRef> branches)
        {
            List<GitItem> gitItems = new List<GitItem>();

            foreach (var branch in branches)
            {
                var items = await gitClient.GetItemsAsync(Context.ProjectName, Context.RepoName, null, VersionControlRecursionType.Full, null, false, false, false, new GitVersionDescriptor { Version = branch.Name.Replace("refs/heads/", ""), VersionType = GitVersionType.Branch });

                gitItems.AddRange(items);
            }


            return gitItems;
        }

        public async Task<GitPullRequest> GetPullRequest(GitRef newBranch)
        {

            GitRepository repo = await GetRepo();

            var res = await gitClient.GetPullRequestsAsync(repo.ProjectReference.Id, repo.Id, new GitPullRequestSearchCriteria { SourceRefName = newBranch.Name, TargetRefName = repo.DefaultBranch });

            return res.FirstOrDefault();
        }


        public async Task<GitPullRequest> CreatePullRequest(GitRef newBranch, string title, string description, WorkItem workItem)
        {

            GitRepository repo = await GetRepo();

            var pr = new GitPullRequest()
            {
                SourceRefName = newBranch.Name,
                TargetRefName = repo.DefaultBranch,
                Title = title,
                Description = description,
            };
            pr.WorkItemRefs = new ResourceRef[] { new ResourceRef { Id = workItem.Id.ToString(), Url = workItem.Url } };

            var res = await gitClient.CreatePullRequestAsync(pr,
            repo.Id);

            return res;
        }

        public async Task<WorkItem> getWorkItem(int Id)
        {
            WorkItemTrackingHttpClient witClient = Connection.GetClient<WorkItemTrackingHttpClient>();
            WorkItem workitem = await witClient.GetWorkItemAsync(Id);
            return workitem;
        }

    }
}
