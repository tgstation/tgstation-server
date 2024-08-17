// Only check jobs that start with these.
// Helps make sure we don't restart something like which is not known to be flaky.
const CONSIDERED_JOBS = [
	"Windows Live Tests",
	"Linux Live Tests",
	"Build .deb Package"
];

async function getFailedJobsForRun(github, context, workflowRunId, runAttempt) {
	const jobs = await github.paginate(
		github.rest.actions.listJobsForWorkflowRunAttempt,
		{
			owner: context.repo.owner,
			repo: context.repo.repo,
			run_id: workflowRunId,
			attempt_number: runAttempt
		},
		response => {
			return response.data;
		});

	return jobs
		.filter((job) => job.conclusion === "failure");
}

export async function rerunFlakyTests({ github, context }) {
	const failingJobs = await getFailedJobsForRun(
		github,
		context,
		context.payload.workflow_run.id,
		context.payload.workflow_run.run_attempt
	);

	if (failingJobs.length > 3) {
		console.log("Many jobs failing. PROBABLY not flaky, not rerunning.");
		return;
	}

	const filteredFailingJobs = failingJobs.filter((job) => {
		console.log(`Failing job: ${job.name}`)
		return CONSIDERED_JOBS.some((title) => job.name.startsWith(title));
	});
	if (filteredFailingJobs.length !== failingJobs.length) {
		console.log("One or more failing jobs are NOT designated flaky. Not rerunning.");
		return;
	}

	console.log(`Rerunning job: ${filteredFailingJobs[0].name}`);

	github.rest.actions.reRunWorkflowFailedJobs({
		owner: context.repo.owner,
		repo: context.repo.repo,
		run_id: context.payload.workflow_run.id,
	});
}
