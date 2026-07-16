import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { getAgentHierarchy } from '../api/accountsApi';
import type { AgentHierarchyNode } from '../types/accounts';

function HierarchyNode({ node }: { node: AgentHierarchyNode }) {
  const typeColour = {
    Master:   'text-purple-700 bg-purple-50',
    Agent:    'text-blue-700 bg-blue-50',
    SubAgent: 'text-gray-700 bg-gray-100',
  }[node.agentType] ?? 'text-gray-700';

  return (
    <li>
      <div className="flex items-center gap-2 py-1">
        <span className={`px-2 py-0.5 rounded text-xs font-medium ${typeColour}`}>
          {node.agentType}
        </span>
        <span className="text-sm font-medium text-gray-800">{node.loginName}</span>
        {node.name && <span className="text-sm text-gray-500">({node.name})</span>}
      </div>
      {node.children.length > 0 && (
        <ul className="ml-6 border-l border-gray-200 pl-4 space-y-1">
          {node.children.map((child) => (
            <HierarchyNode key={child.id} node={child} />
          ))}
        </ul>
      )}
    </li>
  );
}

export default function AgentHierarchyPage() {
  const { id }   = useParams<{ id: string }>();
  const agentId  = parseInt(id ?? '0', 10);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['agent-hierarchy', agentId],
    queryFn:  () => getAgentHierarchy(agentId),
    enabled:  agentId > 0,
  });

  return (
    <div>
      <h1 className="text-xl font-semibold text-gray-800 mb-6">Agent Hierarchy</h1>
      {isLoading && <p className="text-sm text-gray-500">Loading...</p>}
      {isError   && <p className="text-sm text-red-500">Failed to load hierarchy.</p>}
      {data && (
        <div className="bg-white rounded-lg shadow p-6">
          <ul>
            <HierarchyNode node={data} />
          </ul>
        </div>
      )}
    </div>
  );
}
