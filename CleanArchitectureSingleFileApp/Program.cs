using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

//Nödvändiga beroenden för API delen, Controllers, OpenApi/Scalar
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Registrera beroenden (Application & Infrastructure) 
// Kan ske i extention methods som ligger i respektive projekt

//Registration Infra beroenden 
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();

//Registrera Application beroenden
builder.Services.AddTransient<IProductService, ProductService>();



var app = builder.Build();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    _ = endpoints.MapControllers();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service; //<--- Presentationslagret pratar bara med applikationslagret

    public ProductsController(IProductService productService)
    {
        _service = productService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Product>> Get() //Product ligger i Domänlagret så det är okej, men problemet här är att det är samma som databasen använder, egentligen vill man ha DTOs eller "Contracts" så man kan välja vad man vill exponera utåt
    {
        return Ok(_service.GetAllProducts());
    }

    [HttpPost]
    public ActionResult<Product> Post(Product product)
    {
        _service.AddProduct(product);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }
}

// --- Domänlagret --- 
// Domänlagret får inte bero på något annat lager.
// Exempel på saker man kan ha i domänlagret är: Entiteter/modeller, värdeobjekt som används i entiterna/modellerna t.ex Enums eller annat som är kopplat till domänlogiken, tjänster för domänlogik, interfaces
public class Product
{
    private static int _nextId = 1;

    public Product()
    {
        Id = _nextId++;
    }

    public int Id { get; private set; }
    public string Name { get; set; }
}

// Repository interfacet
public interface IProductRepository
{
    IEnumerable<Product> GetAll();
    void Add(Product product);
}

// --- Applikationslagret ---
public interface IProductService // <-- Applikations "use cases" som används i presentations lagret i detta fall en API controller
{
    IEnumerable<Product> GetAllProducts();
    void AddProduct(Product product);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _repository; //<--- Applikationslagret får bero på Domänlagret

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<Product> GetAllProducts()
    {
        return _repository.GetAll();
    }

    public void AddProduct(Product product)
    {
        _repository.Add(product);
    }
}


// --- Infrastruktur-lagret ---
public class InMemoryProductRepository : IProductRepository // <-- Infra får bero på Domän, men inte applikations lagret
{
    //"Databasen"
    private readonly List<Product> _products = [];

    public InMemoryProductRepository()
    {
        //Seed data
        _products.Add(new Product { Name = "Product 1" });
        _products.Add(new Product { Name = "Product 2" });
        _products.Add(new Product { Name = "Product 3" });
    }

    public IEnumerable<Product> GetAll()
    {
        return _products;
    }

    public void Add(Product product)
    {
        _products.Add(product);
    }
}
