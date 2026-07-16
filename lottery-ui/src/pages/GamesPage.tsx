import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { lotteryApi, LotteryGame } from '../api/lottery';

const GAME_TYPE_LABEL: Record<number, string> = { 1: 'Pick 3', 2: 'Pick 4' };

export default function GamesPage() {
  const { data: games, isLoading, error } = useQuery({
    queryKey: ['lottery-games'],
    queryFn: lotteryApi.getGames,
  });

  if (isLoading) return <div className="p-8 text-gray-500">Loading games...</div>;
  if (error) return <div className="p-8 text-red-600">Failed to load lottery games.</div>;

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-indigo-700 text-white px-8 py-4 flex items-center justify-between">
        <h1 className="text-2xl font-bold">COG Lottery</h1>
        <Link to="/history" className="text-indigo-200 hover:text-white text-sm">
          My Tickets
        </Link>
      </header>

      <main className="max-w-2xl mx-auto p-8">
        <h2 className="text-xl font-semibold text-gray-800 mb-6">Select a Game</h2>
        <div className="grid gap-4">
          {games?.map((game: LotteryGame) => (
            <Link
              key={game.id}
              to={`/games/${game.id}/pick`}
              className="block bg-white rounded-lg border border-gray-200 p-6 hover:border-indigo-400 hover:shadow-md transition-all"
            >
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-lg font-semibold text-gray-900">{game.name}</h3>
                  <p className="text-sm text-gray-500 mt-1">
                    {GAME_TYPE_LABEL[game.gameType]} — Choose your numbers
                  </p>
                </div>
                <span className="text-indigo-600 text-2xl font-bold">
                  {GAME_TYPE_LABEL[game.gameType]}
                </span>
              </div>
            </Link>
          ))}
        </div>
      </main>
    </div>
  );
}
