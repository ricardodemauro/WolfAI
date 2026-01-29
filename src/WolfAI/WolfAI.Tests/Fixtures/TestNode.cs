using WolfAI.Core.Domain.Execution;
using WolfAI.Core.Domain.Nodes;
using WolfAI.Core.Domain.Results;

namespace WolfAI.Tests.Fixtures;

/// <summary>
/// Test implementation of a concrete node.
/// </summary>
public class TestNode : Node
{
    private readonly Func<IExecutionContext, CancellationToken, Task<NodeResult>>? _executeLogic;

    public TestNode(
        string id,
        string name,
        NodeType nodeType = NodeType.AI,
        Func<IExecutionContext, CancellationToken, Task<NodeResult>>? executeLogic = null)
        : base(id, name)
    {
        TestNodeType = nodeType;
        _executeLogic = executeLogic;
    }

    public NodeType TestNodeType { get; }

    public override NodeType NodeType => TestNodeType;

    public override async Task<NodeResult> ExecuteAsync(
        IExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (_executeLogic != null)
        {
            return await _executeLogic(context, cancellationToken);
        }

        return NodeResult.SuccessResult();
    }
}
