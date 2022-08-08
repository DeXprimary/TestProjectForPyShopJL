using System;
using Xunit;
using Task1;
namespace TestProjectForPyShopJL.Test
{
    public class GameTest
    {
        Game game;

        public GameTest()
        {
            GameStamp stamp1 = new GameStamp(20, 2, 3);
            GameStamp stamp2 = new GameStamp(100, 8, 15);
            GameStamp stamp3 = new GameStamp(100, 18, 44);
            game = new Game(new GameStamp[] { stamp1, stamp2, stamp3 });
        }

        [Fact]
        public void GetScore_InvalidProperty_Throw()
        {           
            Func<object> funcGetScore = () => game.getScore(21);

            Assert.Throws<InvalidOperationException>(funcGetScore);
        }

        [Fact]
        public void GetScore_ValidProperty_ValidScore()
        {
            Score score = game.getScore(20);

            Assert.Equal<Score>(new Score(2, 3), score);
        }

        [Fact]
        public void GetScore_TwoIdenticalOffset_Throw()
        {
            Func<object> funcGetScore = () => game.getScore(100);

            Assert.Throws<InvalidOperationException>(funcGetScore);
        }
    }
}
