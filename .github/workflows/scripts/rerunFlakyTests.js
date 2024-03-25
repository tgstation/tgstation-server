// Only check jobs that start with these.
// Helps make sure we don't restart something like which is not known to be flaky.
const CONSIDERED_JOBS = [
	"Windows Live Tests",
	"Linux Live Tests",
];

async function getFailedJobsForRun(github, context, workflowRunId, runAttempt) {
	const {
		data: { jobs },
	} = await github.rest.actions.listJobsForWorkflowRunAttempt({
		owner: context.repo.owner,
		repo: context.repo.repo,
		run_id: workflowRunId,
		attempt_number: runAttempt,
	});

	return jobs
		.filter((job) => job.conclusion === "failure")
		.filter((job) =>
			CONSIDERED_JOBS.some((title) => job.name.startsWith(title))
		);
}

export async function rerunFlakyTests({ github, context }) {
	const failingJobs = await getFailedJobsForRun(
		github,
		context,
		context.payload.workflow_run.id,
		context.payload.workflow_run.run_attempt
	);

	if (failingJobs.length > 1) {
		console.log("Multiple jobs failing. PROBABLY not flaky, not rerunning.");
		return;
	}

	if (failingJobs.length === 0) {
		throw new Error(
			"rerunFlakyTests should not have run on a run with no failing jobs"
		);
	}

	github.rest.actions.reRunWorkflowFailedJobs({
		owner: context.repo.owner,
		repo: context.repo.repo,
		run_id: context.payload.workflow_run.id,
	});
}
