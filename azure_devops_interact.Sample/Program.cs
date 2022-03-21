// See https://aka.ms/new-console-template for more information

using azure_devops_interact;
using console_bootstrap;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

try
{

    var pat = "Please provide personal access token for devops".ask();

    "Connecting azure devops".log();
    VssConnection vsconnection = new VssConnection(new Uri("https://dev.azure.com/KrauseGroup"), new VssBasicCredential("", pat));

    AzureGit git = new AzureGit(vsconnection);

    git.Context.ProjectName = "Please enter azure devops project name".ask();

    string storyId = "Please enter the story id".ask();

    //Get workitem from devops
    var workItem = await git.getWorkItem(Convert.ToInt32(storyId));


    if (workItem == null)
        $"Workitem not found with id: {storyId}".exception();

    workItem.Fields["System.Title"].ToString().success();


    git.Context.RepoName = "Please enter repo name".ask();

    //Get repo in current context
    var repo = await git.GetRepo();

    var branchName = "Please enter new branch name".ask();
    //create new branch in current context
    var branchUpdate = await git.CreateBranch(branchName);

    //Get branch by name
    var branch = await git.GetBranch(branchName);

    //Commit changes to branch
    await git.CommitFiles(branch, $"Sample commit", new List<Tuple<string, string>> { Tuple.Create("folder1/file1.cs", "Sample code here") });

    //Get existing pull request from branch to default branch
    var pr = await git.GetPullRequest(branch);

    //create new pr
    var newPr = await git.CreatePullRequest(branch, $"Pr title", "Pr description", workItem);

    //get branch url
    var branchUrl = git.GetBranchUrl(branch);

    //get pr url
    var prUrl = git.GetPrUrl(newPr);

}
catch (Exception ex)
{
    ex.error();
}
