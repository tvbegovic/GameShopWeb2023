using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace GameshopWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public OrderController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpPost("")]
        public Order Create(Order order)
        {
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                db.Open();
                var tr = db.BeginTransaction();
                try
                {
                    order.IdUser = db.ExecuteScalar<int>(
                        @"INSERT INTO [User] (firstname, lastname, address, email, City) OUTPUT inserted.id
                        VALUES(@firstname, @lastname, @address, @email, @City)", order.User,tr
                        );
                    order.DateOrdered = DateTime.Now;
                    order.Id = db.ExecuteScalar<int>(
                        "INSERT INTO [Order](idUser, dateOrdered) OUTPUT inserted.id VALUES(@idUser, @dateOrdered)", order, tr
                        );
                    foreach (var od in order.Details)
                    {
                        od.IdOrder = order.Id;
                        db.Execute(
                         @"INSERT INTO OrderDetail(idGame, idOrder, quantity, unitprice) 
                        VALUES(@idGame, @idOrder, @quantity, @unitprice)", od, tr
                            );
                    }
                    tr.Commit();
                    return order;
                }
                catch (Exception)
                {
                    tr.Rollback();
                    throw;
                }
            
            }
        }
    }
}
