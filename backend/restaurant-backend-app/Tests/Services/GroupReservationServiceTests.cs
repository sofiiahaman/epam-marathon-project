using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.GroupReservation;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using APIHandler.Repositories.Tables;
using APIHandler.Services.Auth;
using APIHandler.Services.GroupReservations;
using APIHandler.Services.Locations;
using APIHandler.Services.GuestSessions;
using AutoFixture.Xunit2;
using Moq;

namespace APIHandler.Tests.Services;

public class GroupReservationServiceTests
{
    private readonly Mock<IGroupReservationRepository> _groupReservationRepositoryMock = new();
    private readonly Mock<ITableRepository> _tableRepositoryMock = new();
    private readonly Mock<IReservationRepository> _reservationRepositoryMock = new();
    private readonly Mock<IGuestSessionRepository> _guestSessionRepositoryMock = new();
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<ILocationService> _locationServiceMock = new();
    private readonly Mock<IGuestSessionService> _guestSessionServiceMock = new();

    private readonly GroupReservationService _service;

    public GroupReservationServiceTests()
    {
        _service = new GroupReservationService(
            _groupReservationRepositoryMock.Object,
            _tableRepositoryMock.Object,
            _reservationRepositoryMock.Object,
            _guestSessionRepositoryMock.Object,
            _guestSessionServiceMock.Object,
            _orderRepositoryMock.Object,
            _userServiceMock.Object,
            _locationServiceMock.Object);
    }

    [Theory]
    [AutoData]
    public async Task GetGroupReservationByTokenAsync_ValidInput_ReturnsGroupReservationInfo(
        GroupReservation groupReservation,
        Table table,
        Reservation reservation,
        Location location,
        string expectedUserName)
    {
        groupReservation.ReservationId = reservation.Id;
        groupReservation.Status = GroupReservationStatus.Open;
        reservation.TableId = table.Id;
        table.LocationId = location.Id;

        _groupReservationRepositoryMock
            .Setup(r => r.GetGroupReservationByTokenAsync(groupReservation.ReservationToken))
            .ReturnsAsync(groupReservation);

        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        _guestSessionRepositoryMock
            .Setup(r => r.CountByGroupReservationId(groupReservation.GroupReservationId))
            .ReturnsAsync(0);

        _tableRepositoryMock
            .Setup(r => r.GetTableAsync(table.Id))
            .ReturnsAsync(table);

        _locationServiceMock
            .Setup(s => s.GetLocationByIdAsync(table.LocationId))
            .ReturnsAsync(location);

        _userServiceMock
            .Setup(s => s.GetUserName(reservation.CustomerId!))
            .ReturnsAsync(expectedUserName);

        var result = await _service.GetGroupReservationByTokenAsync(groupReservation.ReservationToken);

        Assert.Equal(groupReservation.GroupReservationId, result.GroupReservationId);
        Assert.Equal(table.LocationId, result.LocationId);
        Assert.Equal(reservation.GuestCount, result.MaxGuestCount);
        Assert.Equal(reservation.StartTime, result.StartTime);
        Assert.Equal(reservation.EndTime, result.EndTime);
        Assert.Equal(table.TableNumber, result.TableNumber);
        Assert.Equal("Green & Tasty", result.RestaurantName);
        Assert.Equal(location.Address, result.LocationAddress);
        Assert.Equal(expectedUserName, result.InviterName);
    }

    [Theory]
    [AutoData]
    public async Task GetGroupReservationByTokenAsync_InvalidToken_ThrowsNotFound(string token)
    {
        _groupReservationRepositoryMock
            .Setup(r => r.GetGroupReservationByTokenAsync(token))
            .ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => _service.GetGroupReservationByTokenAsync(token));
    }

    [Theory]
    [InlineAutoData(GroupReservationStatus.Closed)]
    [InlineAutoData(GroupReservationStatus.Submitted)]
    public async Task GetGroupReservationByTokenAsync_GroupReservationIsNotOpen_ThrowsException(
        GroupReservationStatus status,
        GroupReservation groupReservation)
    {
        groupReservation.Status = status;

        _groupReservationRepositoryMock
            .Setup(r => r.GetGroupReservationByTokenAsync(groupReservation.ReservationToken))
            .ReturnsAsync(groupReservation);

        await Assert.ThrowsAsync<GroupReservationExpiredException>(() => _service.GetGroupReservationByTokenAsync(groupReservation.ReservationToken));
    }

    [Theory]
    [AutoData]
    public async Task GetGroupReservationByTokenAsync_WhenGuestCountIsMax_ThrowsGuestLimitExceeded(GroupReservation groupReservation, Reservation reservation)
    {
        groupReservation.Status = GroupReservationStatus.Open;
        groupReservation.ReservationId = reservation.Id;

        _groupReservationRepositoryMock
            .Setup(r => r.GetGroupReservationByTokenAsync(groupReservation.ReservationToken))
            .ReturnsAsync(groupReservation);

        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        _guestSessionRepositoryMock
            .Setup(r => r.CountByGroupReservationId(groupReservation.GroupReservationId))
            .ReturnsAsync(reservation.GuestCount);

        await Assert.ThrowsAsync<GuestLimitExceededException>(() => _service.GetGroupReservationByTokenAsync(groupReservation.ReservationToken));
    }

    [Theory]
    [AutoData]
    public async Task CloseGroupReservationAsync_ValidInput_UpdatesGroupReservationStatus(GroupReservation groupReservation)
    {
        _groupReservationRepositoryMock
            .Setup(r => r.GetByReservationIdAsync(groupReservation.ReservationId))
            .ReturnsAsync(groupReservation);

        GroupReservation? savedGroupReservation = null;
        _groupReservationRepositoryMock
            .Setup(r => r.SaveGroupReservationAsync(It.IsAny<GroupReservation>()))
            .Callback<GroupReservation>(r => savedGroupReservation = r);

        await _service.CloseGroupReservationAsync(groupReservation.ReservationId);

        Assert.NotNull(savedGroupReservation);
        Assert.Equal(GroupReservationStatus.Closed, savedGroupReservation.Status);
    }

    [Theory]
    [AutoData]
    public async Task CloseGroupReservationAsync_GroupReservationNotFound_DoesNotThrowException(string reservationId)
    {
        _groupReservationRepositoryMock
            .Setup(r => r.GetByReservationIdAsync(reservationId))
            .ReturnsAsync((GroupReservation?)null);

        await _service.CloseGroupReservationAsync(reservationId);
    }

    [Fact]
    public async Task CreateGroupReservationAsync_CreatesOwnerSession()
    {
        var reservation = new Reservation
        {
            Id = "reservation",
            CustomerId = "owner",
            TableId = "table",
            WaiterId = "waiter"
        };
        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        await _service.CreateGroupReservationAsync(reservation.Id);

        _guestSessionServiceMock.Verify(
            s => s.CreateGroupOwnerSessionAsync("owner", It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateGroupReservationIfDoesNotExistAsync_ExistingGroupEnsuresOwnerSessionWithoutCreatingGroup()
    {
        var group = new GroupReservation
        {
            GroupReservationId = "group",
            ReservationId = "reservation",
            ReservationToken = "token"
        };
        _groupReservationRepositoryMock
            .Setup(r => r.GetByReservationIdAsync(group.ReservationId))
            .ReturnsAsync(group);

        await _service.CreateGroupReservationIfDoesNotExistAsync("owner", group.ReservationId);

        _guestSessionServiceMock.Verify(
            s => s.CreateGroupOwnerSessionAsync("owner", group.GroupReservationId),
            Times.Once);
        _groupReservationRepositoryMock.Verify(
            r => r.SaveGroupReservationAsync(It.IsAny<GroupReservation>()),
            Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task GetGroupReservationInviteTokenAsync_ValidInput_ReturnsToken(Reservation reservation, GroupReservation groupReservation)
    {
        reservation.Status = ReservationStatus.Reserved;
        reservation.GuestCount = 3;
        groupReservation.ReservationId = reservation.Id;
        groupReservation.Status = GroupReservationStatus.Open;

        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        _groupReservationRepositoryMock
            .Setup(r => r.GetByReservationIdAsync(reservation.Id))
            .ReturnsAsync(groupReservation);

        GetGroupInviteTokenResponse result = await _service.GetGroupReservationInviteTokenAsync(reservation.Id);

        Assert.Equal(groupReservation.ReservationToken, result.Token);
    }

    [Theory]
    [AutoData]
    public async Task GetGroupReservationInviteTokenAsync_InvalidReservationId_ThrowsNotFound(string reservationId)
    {
        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync((Reservation?)null);

        await Assert.ThrowsAsync<ReservationNotFoundException>(() => _service.GetGroupReservationInviteTokenAsync(reservationId));
    }

    [Theory]
    [AutoData]
    public async Task GetGroupReservationInviteTokenAsync_OneGuest_ThrowsValidationException(Reservation reservation)
    {
        reservation.Status = ReservationStatus.Reserved;
        reservation.GuestCount = 1;

        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        await Assert.ThrowsAsync<ValidationException>(() => _service.GetGroupReservationInviteTokenAsync(reservation.Id));
    }

    [Theory]
    [InlineAutoData(ReservationStatus.Completed)]
    [InlineAutoData(ReservationStatus.Cancelled)]
    [InlineAutoData(ReservationStatus.InProgress)]
    public async Task GetGroupReservationInviteTokenAsync_StatusIsNotReserved_ThrowsValidationException(ReservationStatus reservationStatus, Reservation reservation)
    {
        reservation.Status = reservationStatus;
        reservation.GuestCount = 1;

        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        await Assert.ThrowsAsync<ValidationException>(() => _service.GetGroupReservationInviteTokenAsync(reservation.Id));
    }

    [Theory]
    [InlineAutoData(GroupReservationStatus.Closed, typeof(GroupReservationExpiredException))]
    [InlineAutoData(GroupReservationStatus.Submitted, typeof(GroupReservationSubmittedException))]
    public async Task GetGroupReservationInviteTokenAsync_GroupStatusIsNotOpen_ThrowsException(GroupReservationStatus status, Type type, Reservation reservation, GroupReservation groupReservation)
    {
        reservation.Status = ReservationStatus.Reserved;
        reservation.GuestCount = 3;
        groupReservation.ReservationId = reservation.Id;
        groupReservation.Status = status;

        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        _groupReservationRepositoryMock
            .Setup(r => r.GetByReservationIdAsync(reservation.Id))
            .ReturnsAsync(groupReservation);

        await Assert.ThrowsAsync(type, () => _service.GetGroupReservationInviteTokenAsync(reservation.Id));
    }

    [Fact]
    public async Task SubmitGroupReservationAsync_CombinesSubmittedDishesAndUpdatesOrderAndGroup()
    {
        var reservation = new Reservation { Id = "reservation", CustomerId = "owner", TableId = "table", WaiterId = "waiter" };
        var group = new GroupReservation
        {
            GroupReservationId = "group",
            ReservationId = reservation.Id,
            ReservationToken = "token",
            Status = GroupReservationStatus.Open
        };
        var order = new Order
        {
            OrderId = "order",
            CustomerId = "owner",
            ReservationId = reservation.Id,
            ReservationAddress = "address",
            ReservationTime = "time",
            Dishes = []
        };
        _groupReservationRepositoryMock.Setup(r => r.GetGroupReservationByIdAsync(group.GroupReservationId)).ReturnsAsync(group);
        _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(reservation.Id)).ReturnsAsync(reservation);
        _orderRepositoryMock.Setup(r => r.GetOrderByReservationIdAsync(reservation.Id)).ReturnsAsync(order);
        _guestSessionRepositoryMock.Setup(r => r.GetAllByGroupReservationIdAsync(group.GroupReservationId)).ReturnsAsync([
            new GuestSession { Id = "one", GroupReservationId = group.GroupReservationId, GuestName = "One", IsSubmitted = true, Dishes = [new OrderItem { DishId = "dish", Quantity = 2 }] },
            new GuestSession { Id = "two", GroupReservationId = group.GroupReservationId, GuestName = "Two", IsSubmitted = true, Dishes = [new OrderItem { DishId = "dish", Quantity = 3 }, new OrderItem { DishId = "other", Quantity = 1 }] },
            new GuestSession { Id = "three", GroupReservationId = group.GroupReservationId, GuestName = "Three", IsSubmitted = false, Dishes = [new OrderItem { DishId = "ignored", Quantity = 9 }] }
        ]);

        var result = (await _service.SubmitGroupReservationAsync(
            "owner", group.GroupReservationId, new SubmitGroupReservationRequest(reservation.Id))).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(5, result.Single(item => item.DishId == "dish").Quantity);
        Assert.Equal(1, result.Single(item => item.DishId == "other").Quantity);
        Assert.Equal(OrderStatus.SubmittedByCustomer, order.Status);
        Assert.Equal(GroupReservationStatus.Submitted, group.Status);
        _orderRepositoryMock.Verify(r => r.SaveOrderAsync(order), Times.Once);
        _groupReservationRepositoryMock.Verify(r => r.SaveGroupReservationAsync(group), Times.Once);
    }

    [Fact]
    public async Task SubmitGroupReservationAsync_RejectsMissingOrder()
    {
        var (group, reservation) = CreateSubmitFixture();
        _orderRepositoryMock.Setup(r => r.GetOrderByReservationIdAsync(reservation.Id)).ReturnsAsync((Order?)null);
        SetupSubmitFixture(group, reservation);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.SubmitGroupReservationAsync("owner", group.GroupReservationId, new SubmitGroupReservationRequest(reservation.Id)));
    }

    [Fact]
    public async Task SubmitGroupReservationAsync_RejectsMissingGroup()
    {
        var (group, reservation) = CreateSubmitFixture();
        _groupReservationRepositoryMock
            .Setup(r => r.GetGroupReservationByIdAsync(group.GroupReservationId))
            .ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() =>
            _service.SubmitGroupReservationAsync("owner", group.GroupReservationId, new SubmitGroupReservationRequest(reservation.Id)));
    }

    [Fact]
    public async Task SubmitGroupReservationAsync_RejectsMissingReservation()
    {
        var (group, reservation) = CreateSubmitFixture();
        _groupReservationRepositoryMock
            .Setup(r => r.GetGroupReservationByIdAsync(group.GroupReservationId))
            .ReturnsAsync(group);
        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync((Reservation?)null);

        await Assert.ThrowsAsync<ReservationNotFoundException>(() =>
            _service.SubmitGroupReservationAsync("owner", group.GroupReservationId, new SubmitGroupReservationRequest(reservation.Id)));
    }

    [Fact]
    public async Task SubmitGroupReservationAsync_RejectsNonOwner()
    {
        var (group, reservation) = CreateSubmitFixture();
        SetupSubmitFixture(group, reservation);

        await Assert.ThrowsAsync<ActionForbiddenException>(() =>
            _service.SubmitGroupReservationAsync("guest", group.GroupReservationId, new SubmitGroupReservationRequest(reservation.Id)));
    }

    [Fact]
    public async Task SubmitGroupReservationAsync_IncludesSubmittedOwnerSession()
    {
        var (group, reservation) = CreateSubmitFixture();
        SetupSubmitFixture(group, reservation, [
            new GuestSession
            {
                Id = "owner-session",
                GroupReservationId = group.GroupReservationId,
                GuestName = "Owner",
                IsOwnerSession = true,
                OwnerUserId = reservation.CustomerId,
                IsSubmitted = true,
                Dishes = [new OrderItem { DishId = "owner-dish", Quantity = 2 }]
            }
        ]);
        _orderRepositoryMock.Setup(r => r.GetOrderByReservationIdAsync(reservation.Id)).ReturnsAsync(CreateOrder(reservation.Id));

        var result = await _service.SubmitGroupReservationAsync(
            reservation.CustomerId, group.GroupReservationId, new SubmitGroupReservationRequest(reservation.Id));

        Assert.Equal("owner-dish", Assert.Single(result).DishId);
    }

    [Fact]
    public async Task SubmitGroupReservationAsync_RejectsUnsubmittedSessions()
    {
        var (group, reservation) = CreateSubmitFixture();
        SetupSubmitFixture(group, reservation, [
            new GuestSession { Id = "guest", GroupReservationId = group.GroupReservationId, GuestName = "Guest", Dishes = [], IsSubmitted = false }
        ]);
        _orderRepositoryMock.Setup(r => r.GetOrderByReservationIdAsync(reservation.Id)).ReturnsAsync(CreateOrder(reservation.Id));

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.SubmitGroupReservationAsync("owner", group.GroupReservationId, new SubmitGroupReservationRequest(reservation.Id)));
    }

    [Fact]
    public async Task SubmitGroupReservationAsync_RejectsSubmittedSessionsWithoutDishes()
    {
        var (group, reservation) = CreateSubmitFixture();
        SetupSubmitFixture(group, reservation, [
            new GuestSession { Id = "guest", GroupReservationId = group.GroupReservationId, GuestName = "Guest", Dishes = [], IsSubmitted = true }
        ]);
        _orderRepositoryMock.Setup(r => r.GetOrderByReservationIdAsync(reservation.Id)).ReturnsAsync(CreateOrder(reservation.Id));

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.SubmitGroupReservationAsync("owner", group.GroupReservationId, new SubmitGroupReservationRequest(reservation.Id)));
    }

    [Fact]
    public async Task SubmitGroupReservationAsync_RejectsSecondSubmit()
    {
        var (group, reservation) = CreateSubmitFixture();
        group.Status = GroupReservationStatus.Submitted;
        _groupReservationRepositoryMock.Setup(r => r.GetGroupReservationByIdAsync(group.GroupReservationId)).ReturnsAsync(group);

        await Assert.ThrowsAsync<GroupReservationSubmittedException>(() =>
            _service.SubmitGroupReservationAsync("owner", group.GroupReservationId, new SubmitGroupReservationRequest(reservation.Id)));
    }

    [Fact]
    public async Task SubmitGroupReservationAsync_RejectsReservationFromAnotherGroup()
    {
        var (group, reservation) = CreateSubmitFixture();
        var otherReservation = new Reservation { Id = "other", CustomerId = "owner", TableId = "table", WaiterId = "waiter" };
        SetupSubmitFixture(group, reservation);
        _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(otherReservation.Id)).ReturnsAsync(otherReservation);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.SubmitGroupReservationAsync("owner", group.GroupReservationId, new SubmitGroupReservationRequest(otherReservation.Id)));
    }

    private (GroupReservation Group, Reservation Reservation) CreateSubmitFixture()
    {
        var reservation = new Reservation { Id = "reservation", CustomerId = "owner", TableId = "table", WaiterId = "waiter" };
        var group = new GroupReservation { GroupReservationId = "group", ReservationId = reservation.Id, ReservationToken = "token", Status = GroupReservationStatus.Open };
        return (group, reservation);
    }

    private void SetupSubmitFixture(GroupReservation group, Reservation reservation, List<GuestSession>? sessions = null)
    {
        _groupReservationRepositoryMock.Setup(r => r.GetGroupReservationByIdAsync(group.GroupReservationId)).ReturnsAsync(group);
        _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(reservation.Id)).ReturnsAsync(reservation);
        _guestSessionRepositoryMock.Setup(r => r.GetAllByGroupReservationIdAsync(group.GroupReservationId)).ReturnsAsync(sessions ?? []);
    }

    private static Order CreateOrder(string reservationId) => new()
    {
        OrderId = "order",
        CustomerId = "owner",
        ReservationId = reservationId,
        ReservationAddress = "address",
        ReservationTime = "time",
        Dishes = []
    };
}
