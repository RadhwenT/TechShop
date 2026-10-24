using Microsoft.Data.SqlClient;


namespace TechShop.Models.DAO
{
    public class CategorieDAO
    {
        private string chainecnx;
        public CategorieDAO(IConfiguration configuration)
        {
            chainecnx = configuration.GetConnectionString("cnx");
        }

        public List<Categorie> GetAll()
        {
            List<Categorie> categories = new List<Categorie>();
            SqlConnection cnx = new SqlConnection(chainecnx);

            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand("SELECT Id, Name FROM Categories WHERE IsActive = 1", cnx);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Categorie categorie = new Categorie()
                    {
                        Id = dr.GetFieldValue<int>(0),
                        Name = dr.GetFieldValue<string>(1)

                    };
                    categories.Add(categorie);

                }

            }
            catch (Exception ex) { throw; }
            finally { cnx.Close(); }
            return categories;

        }

        public List<Categorie> GetByName(string name)
        {
            List<Categorie> categories = new List<Categorie>();
            SqlConnection cnx = new SqlConnection(chainecnx);

            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand("SELECT Id, Name FROM Categories WHERE IsActive = 1 AND Name LIKE @Name", cnx);
                cmd.Parameters.AddWithValue("@Name", name + "%");
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Categorie categorie = new Categorie()
                    {
                        Id = dr.GetFieldValue<int>(0),
                        Name = dr.GetFieldValue<string>(1)

                    };
                    categories.Add(categorie);

                }

            }
            catch (Exception ex) { throw; }
            finally { cnx.Close(); }
            return categories;

        }

        public bool AddCategorie(Categorie categorie)
        {
            SqlConnection cnx = new SqlConnection(chainecnx);
            int n = 0;
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand("INSERT INTO Categories (Name) VALUES (@Name)", cnx);
                cmd.Parameters.AddWithValue("@Name", categorie.Name);
                n = cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { throw; }
            finally { cnx.Close(); }
            if (n > 0) return true;
            return false;

        }

        public bool UpdateCategorie(Categorie categorie)
        {
            SqlConnection cnx = new SqlConnection(chainecnx);
            int n = 0;
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Categories SET Name = @Name where Id = @Id", cnx);
                cmd.Parameters.AddWithValue("@Id", categorie.Id);
                cmd.Parameters.AddWithValue("@Name", categorie.Name);
                n = cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { throw; }
            finally { cnx.Close(); }
            if (n > 0) return true;
            return false;

        }

        public bool DeleteCategorie(int id)
        {
            SqlConnection cnx = new SqlConnection(chainecnx);
            int n = 0;
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Categories SET IsActive = 0 WHERE Id = @Id", cnx);
                cmd.Parameters.AddWithValue("@Id", id);

                n = cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { throw; }
            finally { cnx.Close(); }
            if (n > 0) return true;
            return false;

        }

        public bool ExistsName(string name, int excludeId = 0)
        {
            SqlConnection cnx = new SqlConnection(chainecnx);
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Categories WHERE LOWER(Name) = LOWER(@Name) AND Id <> @ExcludeId",
                    cnx);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@ExcludeId", excludeId);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
            catch (Exception) { throw; }
            finally { cnx.Close(); }
        }

        public List<Categorie> GetDeleted(string name = "")
        {
            List<Categorie> categories = new List<Categorie>();
            SqlConnection cnx = new SqlConnection(chainecnx);
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT Id, Name FROM Categories WHERE IsActive = 0 AND Name LIKE @Name",
                    cnx);
                cmd.Parameters.AddWithValue("@Name", name + "%");
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    categories.Add(new Categorie
                    {
                        Id = dr.GetFieldValue<int>(0),
                        Name = dr.GetFieldValue<string>(1)
                    });
                }
            }
            catch (Exception) { throw; }
            finally { cnx.Close(); }
            return categories;
        }

        
        public bool RestoreCategorie(int id)
        {
            SqlConnection cnx = new SqlConnection(chainecnx);
            int n = 0;
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE Categories SET IsActive = 1 WHERE Id = @Id",
                    cnx);
                cmd.Parameters.AddWithValue("@Id", id);
                n = cmd.ExecuteNonQuery();
            }
            catch (Exception) { throw; }
            finally { cnx.Close(); }
            return n > 0;
        }
    }
}