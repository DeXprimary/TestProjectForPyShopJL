using Grpc.Core;
using Billing;
using GrpcServiceForPyShopJL;
using System.Linq;

namespace GrpcServiceForPyShopJL.Services
{
    #region Users initialization
    public class UserAccount
    {
        public static UserAccount[] users = new UserAccount[]
        {
            new UserAccount(new UserProfile() { Name = "boris", Amount = 0 }, 5000),
            new UserAccount(new UserProfile() { Name = "maria", Amount = 0 }, 1000),
            new UserAccount(new UserProfile() { Name = "oleg", Amount = 0 }, 800)
        };

        public UserProfile User { get; private set; }
        public Stack<CoinEntity> Coins { get; private set; }
        public int Rating { get; private set; }

        public UserAccount(UserProfile user, int rating)
        {
            this.User = user;

            this.Rating = rating;

            this.Coins = new Stack<CoinEntity>();
        }

        public void PushAndRefreshAmount(CoinEntity coin)
        {
            this.Coins.Push(coin);

            this.User.Amount = this.Coins.Count;
        }

        public CoinEntity PopAndRefreshAmount()
        {
            CoinEntity coin = this.Coins.Pop();

            this.User.Amount = this.Coins.Count;

            return coin;
        }
    }

    
    #endregion

    #region Coin Definition
    public class CoinEntity
    {
        static long totalAmount = 0;
        public Coin Coin { get; private set; }

        public CoinEntity(UserAccount targetUser)
        {
            this.Coin = new Coin() { History = targetUser.User.Name, Id = totalAmount };

            totalAmount++;
        }
    }
    #endregion

    public class BillingService : Billing.Billing.BillingBase
    {        
        public override async Task ListUsers(None request, IServerStreamWriter<UserProfile> responseStream, ServerCallContext context)
        {
            for (int k = 0; k < UserAccount.users.Length; k++)
            {
                if (context.CancellationToken.IsCancellationRequested) break;

                await responseStream.WriteAsync(UserAccount.users[k].User);
            }
        }

        public override Task<Response> CoinsEmission(EmissionAmount request, ServerCallContext context)
        {
            // Если число монет для операции эмиссии меньше чем юзеров то вернем FAILED
            if (request.Amount < UserAccount.users.Length) 
                return Task.Run(() => new Response() { Status = Response.Types.Status.Failed, Comment = "Ошибка! Монет меньше чем пользователей!"});

            // Находим суммарный рейтинг пользователей
            long aggregateRating = UserAccount.users
                .Select(obj => obj.Rating)
                .Sum();
            
            // Предварительно раздаём всем по 1 монете, т.к. по ТЗ каждый должен получить минимум 1 монету
            foreach (var user in UserAccount.users) user.PushAndRefreshAmount(new CoinEntity(user));

            // Определяем коэффициент распределения оставшихся монет
            long factor = aggregateRating / (request.Amount - (long)UserAccount.users.Length);

            // Раздаём монеты пропорционально рейтингу
            long totalAwarded = (long)UserAccount.users.Length;

            for (int userIndex = 0; userIndex < UserAccount.users.Length; userIndex++)
            {
                for (int coinIndex = 0; coinIndex < UserAccount.users[userIndex].Rating / factor; coinIndex++)
                {
                    UserAccount.users[userIndex].PushAndRefreshAmount(new CoinEntity(UserAccount.users[userIndex]));

                    totalAwarded++;
                }
            }

            // Оставшиеся нераспределенные монеты раздаём по порядку относительно остатка от деления: users[userIndex].Rating / factor
            var sortedUsers = UserAccount.users
                .OrderByDescending(obj => (obj.Rating / factor))
                .Take((int)(request.Amount - totalAwarded));
            
            foreach (var user in sortedUsers) user.PushAndRefreshAmount(new CoinEntity(user));

            // Так как эмиссия успешна вернём ОК!
            return Task.Run(() => new Response() { Status = Response.Types.Status.Ok, Comment = "ОК! Монеты распределены!" });
        }

        public override Task<Response> MoveCoins(MoveCoinsTransaction request, ServerCallContext context)
        {
            UserAccount sourceUser = UserAccount.users
                .Where(user => user.User.Name == request.SrcUser)
                .Single();

            UserAccount destinationUser = UserAccount.users
                .Where(user => user.User.Name == request.DstUser)
                .Single();

            // Если у отправителя недостаточно монет то возвращаем FAILED
            if (sourceUser.Coins.Count < request.Amount)
                return Task.Run(() => new Response() { Status = Response.Types.Status.Failed, Comment = "Ошибка! У отправителя недостаточно монет!" });

            // Переводим монеты на счёт получателя
            for (int k = 0; k < request.Amount; k++)
            {
                CoinEntity coin = sourceUser.PopAndRefreshAmount();
                              
                destinationUser.PushAndRefreshAmount(coin);

                coin.Coin.History += "-" + destinationUser.User.Name;
            }

            // Так как эмиссия успешна вернём ОК!
            return Task.Run(() => new Response() { Status = Response.Types.Status.Ok, Comment = "ОК! Монеты переведены!" });
        }

        public override Task<Coin> LongestHistoryCoin(None request, ServerCallContext context)
        {
            // Найдём монету с самой длинной историей при помощи LINQ
            Coin longestHistoryCoin = UserAccount.users
                .SelectMany(user => user.Coins)
                .OrderByDescending(coin => coin.Coin.History.Split('-').Length)
                .First().Coin;

            return 
                Task.Run(() => longestHistoryCoin);
        }
    }
}
