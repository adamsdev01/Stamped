let dispatcherRef = null;

export function initWebMcp(dotNetRef) {
    dispatcherRef = dotNetRef;

    if (typeof document.modelContext === 'undefined') {
        console.warn('WebMCP (document.modelContext) is not available in this browser.');
        return false;
    }

    document.modelContext.registerTool({
        name: 'parseResume',
        description: 'Parses the resume PDF the user has uploaded on this page and extracts role, experience, and skills.',
        inputSchema: { type: 'object', properties: {} },
        execute: async () => {
            return await dispatcherRef.invokeMethodAsync('ExecuteTool', 'parseResume', '{}');
        }
    });

    document.modelContext.registerTool({
        name: 'matchJobs',
        description: 'Scores the parsed resume against open job postings and returns ranked matches with matchId values.',
        inputSchema: { type: 'object', properties: {} },
        execute: async () => {
            return await dispatcherRef.invokeMethodAsync('ExecuteTool', 'matchJobs', '{}');
        }
    });

    document.modelContext.registerTool({
        name: 'draftCoverLetter',
        description: 'Drafts a cover letter for a specific job match. Requires the matchId returned by matchJobs.',
        inputSchema: {
            type: 'object',
            properties: {
                matchId: { type: 'number', description: 'The matchId returned by matchJobs' }
            },
            required: ['matchId']
        },
        execute: async (args) => {
            return await dispatcherRef.invokeMethodAsync('ExecuteTool', 'draftCoverLetter', JSON.stringify(args));
        }
    });

    // Deliberately no submitApplication tool. That action stays button-only in the UI —
    // there is no WebMCP registration for it, so no agent path can reach it.

    return true;
}

export function teardownWebMcp() {
    dispatcherRef = null;
    // document.modelContext does not currently expose a documented unregister/removeTool method
    // as of Chrome 152 (WebMCP origin trial). Tools remain registered until the page/document unloads.
    // Revisit this once the spec exposes an unregister API.
}