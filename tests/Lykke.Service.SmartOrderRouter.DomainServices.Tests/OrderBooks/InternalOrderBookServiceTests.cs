using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.MarketInstruments.Client.Models.AssetPairs;
using Lykke.Service.MarketInstruments.Client.Services;
using Lykke.Service.SmartOrderRouter.Domain.Entities.OrderBooks;
using Lykke.Service.SmartOrderRouter.Domain.Services;
using Lykke.Service.SmartOrderRouter.DomainServices.OrderBooks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Lykke.Service.SmartOrderRouter.DomainServices.Tests.OrderBooks
{
    [TestClass]
    public class InternalOrderBookServiceTests
    {
        private readonly Mock<IExternalOrderBookService> _externalOrderBookServiceMock =
            new Mock<IExternalOrderBookService>();

        private readonly Mock<IMarketInstrumentService> _marketInstrumentsServiceMock =
            new Mock<IMarketInstrumentService>();

        private readonly Mock<ISettingsService> _settingsServiceMock =
            new Mock<ISettingsService>();

        private readonly List<AssetPairModel> _assetPairs = new List<AssetPairModel>();

        private readonly List<ExternalOrderBook> _externalOrderBooks = new List<ExternalOrderBook>();

        private InternalOrderBookService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _externalOrderBookServiceMock.Setup(o => o.GetByAssetPair(It.IsAny<string>()))
                .Returns((string assetPair) => _externalOrderBooks.Where(o => o.AssetPair == assetPair).ToList());

            _marketInstrumentsServiceMock.Setup(o => o.GetAssetPair(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string name, string exchange) =>
                    _assetPairs.FirstOrDefault(o => o.Name == name && o.Exchange == exchange));

            _service = new InternalOrderBookService(
                _externalOrderBookServiceMock.Object,
                _marketInstrumentsServiceMock.Object,
                _settingsServiceMock.Object);
        }

        [TestMethod]
        public async Task Create_Order_Book_With_Out_Arbitrage()
        {
            // arrange

            _assetPairs.Add(new AssetPairModel
            {
                Name = "BTCUSD",
                PriceAccuracy = 3
            });

            _externalOrderBooks.AddRange(new[]
            {
                new ExternalOrderBook
                {
                    Exchange = "Exchange1",
                    AssetPair = "BTCUSD",
                    Timestamp = DateTime.UtcNow,
                    SellLevels = new List<ExternalOrderBookLevel>
                    {
                        new ExternalOrderBookLevel {Price = 3.9626635m, Volume = 3},
                        new ExternalOrderBookLevel {Price = 2.1341931m, Volume = 2},
                        new ExternalOrderBookLevel {Price = 1.8762345m, Volume = 1}
                    },
                    BuyLevels = new List<ExternalOrderBookLevel>
                    {
                        new ExternalOrderBookLevel {Price = 1.5761689m, Volume = 1},
                        new ExternalOrderBookLevel {Price = 1.0341931m, Volume = 2},
                        new ExternalOrderBookLevel {Price = 0.5626635m, Volume = 3}
                    }
                },
                new ExternalOrderBook
                {
                    Exchange = "Exchange2",
                    AssetPair = "BTCUSD",
                    Timestamp = DateTime.UtcNow,
                    SellLevels = new List<ExternalOrderBookLevel>
                    {
                        new ExternalOrderBookLevel {Price = 3.963m, Volume = 3},
                        new ExternalOrderBookLevel {Price = 2.136m, Volume = 2},
                        new ExternalOrderBookLevel {Price = 1.877m, Volume = 1}
                    },
                    BuyLevels = new List<ExternalOrderBookLevel>
                    {
                        new ExternalOrderBookLevel {Price = 1.576m, Volume = 1},
                        new ExternalOrderBookLevel {Price = 1.035m, Volume = 2},
                        new ExternalOrderBookLevel {Price = 0.562m, Volume = 3}
                    }
                }
            });

            var expectedInternalOrderBook = new InternalOrderBook
            {
                AssetPair = "BTCUSD",
                Timestamp = DateTime.UtcNow,
                SellLevels = new List<OrderBookLevel>
                {
                    new OrderBookLevel {Price = 3.963m, Volume = 6},
                    new OrderBookLevel {Price = 2.136m, Volume = 2},
                    new OrderBookLevel {Price = 2.135m, Volume = 2},
                    new OrderBookLevel {Price = 1.877m, Volume = 2}
                },
                BuyLevels = new List<OrderBookLevel>
                {
                    new OrderBookLevel {Price = 1.576m, Volume = 2},
                    new OrderBookLevel {Price = 1.034m, Volume = 2},
                    new OrderBookLevel {Price = 1.035m, Volume = 2},
                    new OrderBookLevel {Price = 0.562m, Volume = 6}
                }
            };

            // act

            await _service.UpdateAsync("BTCUSD");

            var actualInternalOrderBook = _service.GetByAssetPair("BTCUSD");

            // assert

            Assert.IsTrue(AreEqual(expectedInternalOrderBook, actualInternalOrderBook));
        }

        [TestMethod]
        public async Task Create_Order_Book_With_Arbitrage()
        {
            // arrange

            _assetPairs.Add(new AssetPairModel
            {
                Name = "BTCUSD",
                PriceAccuracy = 3
            });

            _externalOrderBooks.AddRange(new[]
            {
                new ExternalOrderBook
                {
                    Exchange = "Exchange1",
                    AssetPair = "BTCUSD",
                    Timestamp = DateTime.UtcNow,
                    SellLevels = new List<ExternalOrderBookLevel>
                    {
                        new ExternalOrderBookLevel {Price = 3.9626635m, Volume = 3},
                        new ExternalOrderBookLevel {Price = 2.1341931m, Volume = 2},
                        new ExternalOrderBookLevel {Price = 1.8762345m, Volume = 1}
                    },
                    BuyLevels = new List<ExternalOrderBookLevel>
                    {
                        new ExternalOrderBookLevel {Price = 1.5761689m, Volume = 1},
                        new ExternalOrderBookLevel {Price = 1.0341931m, Volume = 2},
                        new ExternalOrderBookLevel {Price = 0.5626635m, Volume = 3}
                    }
                },
                new ExternalOrderBook
                {
                    Exchange = "Exchange2",
                    AssetPair = "BTCUSD",
                    Timestamp = DateTime.UtcNow,
                    SellLevels = new List<ExternalOrderBookLevel>
                    {
                        new ExternalOrderBookLevel {Price = 3.963m, Volume = 3},
                        new ExternalOrderBookLevel {Price = 3.136m, Volume = 2},
                        new ExternalOrderBookLevel {Price = 2.877m, Volume = 1}
                    },
                    BuyLevels = new List<ExternalOrderBookLevel>
                    {
                        new ExternalOrderBookLevel {Price = 2.576m, Volume = 1},
                        new ExternalOrderBookLevel {Price = 2.035m, Volume = 2},
                        new ExternalOrderBookLevel {Price = 1.562m, Volume = 3}
                    }
                }
            });

            var expectedInternalOrderBook = new InternalOrderBook
            {
                AssetPair = "BTCUSD",
                Timestamp = DateTime.UtcNow,
                SellLevels = new List<OrderBookLevel>
                {
                    new OrderBookLevel {Price = 3.963m, Volume = 6},
                    new OrderBookLevel {Price = 3.136m, Volume = 2},
                    new OrderBookLevel {Price = 2.877m, Volume = 1},
                    new OrderBookLevel {Price = 2.227m, Volume = 3}
                },
                BuyLevels = new List<OrderBookLevel>
                {
                    new OrderBookLevel {Price = 2.225m, Volume = 1},
                    new OrderBookLevel {Price = 2.035m, Volume = 2},
                    new OrderBookLevel {Price = 1.576m, Volume = 1},
                    new OrderBookLevel {Price = 1.562m, Volume = 3},
                    new OrderBookLevel {Price = 1.034m, Volume = 2},
                    new OrderBookLevel {Price = 0.562m, Volume = 3}
                }
            };

            // act

            await _service.UpdateAsync("BTCUSD");

            var actualInternalOrderBook = _service.GetByAssetPair("BTCUSD");

            // assert

            Assert.IsTrue(AreEqual(expectedInternalOrderBook, actualInternalOrderBook));
        }

        private static bool AreEqual(InternalOrderBook a, InternalOrderBook b)
        {
            if (a == null && b == null)
                return true;

            if (a == null || b == null)
                return false;

            if (a.AssetPair != b.AssetPair)
                return false;

            return AreEqual(a.SellLevels, b.SellLevels) && AreEqual(a.BuyLevels, b.BuyLevels);
        }

        private static bool AreEqual(IReadOnlyCollection<OrderBookLevel> a, IReadOnlyCollection<OrderBookLevel> b)
            => a.Count == b.Count && a.All(o => b.Any(p => AreEqual(o, p)));

        private static bool AreEqual(OrderBookLevel a, OrderBookLevel b)
            => a.Price == b.Price && a.Volume == b.Volume;
    }
}
