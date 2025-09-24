import { useQuery } from '@tanstack/react-query';
import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/work-items/')({
  component: RouteComponent,
})

function RouteComponent() {
  const {api} = Route.useRouteContext();

  const { data: task } = useQuery({
    queryKey: ['work-items'],
    queryFn: () => api.tasks.tasksList().then(res => res.data)
  })

  return <div>{JSON.stringify(task)}</div>
}
