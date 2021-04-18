using BoardGames;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace GameServer
{
	public class Program
	{
		public static void Main(string[] args)
		{
			IReadOnlyDictionary<int, IReadOnlyDictionary<int, int>> d = new Dictionary<int, IReadOnlyDictionary<int, int>>
			{
				[1] = new Dictionary<int, int> { [2] = 3 }
			};

			var a = d as Dictionary<int, Dictionary<int, int>>;
			a[4] = new Dictionary<int, int> { [5] = 6 };

			Console.WriteLine(d[4]);



			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				});
	}



	public sealed class Server : ITime
	{
		private readonly History history;
		private readonly IEnumerator<int> playerIDGenerator;


		public Server() => throw new NotImplementedException();

		public int currentPlayerID => playerIDGenerator.Current;


		#region Network Time
		public float elapsedTurnTime => throw new NotImplementedException();
		public float remainTurnTime => maxTurnTime - elapsedTurnTime;
		public float ElapsedPlayerTime(int playerID) => throw new NotImplementedException();
		public float RemainPlayerTime(int playerID) => maxPlayerTimes[playerID] - ElapsedPlayerTime(playerID);

		private float turnStartTime, maxTurnTime;
		private readonly Dictionary<int, float> playerStartTimes = new Dictionary<int, float>();
		private readonly Dictionary<int, float> maxPlayerTimes = new Dictionary<int, float>();


		#endregion
		#region Validations
		private Task CheckTurnBegin() => throw new NotImplementedException();
		private Task CheckTurnEnd() => throw new NotImplementedException();
		private Task CheckPlayerMove(IMoveData data) => throw new NotImplementedException();
		private Task CheckRequest(int playerID, Request request) => throw new NotImplementedException();
		#endregion
	}

}