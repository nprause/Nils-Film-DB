using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nils_Film_DB
{
    struct genres
    {
        String id;
        String name;
    }

    struct production_countries
    {
        String iso_3166_1;
    }

    struct cast
    {
        String name;
    }

    struct crew
    {
        String job;
        String name;
    }

    struct credits
    {
        cast cast;
        crew crew;
    }

    struct titles
    {
        String title;
    }

    struct alternative_titles
    {
        titles titles;
    }

    struct movie_detail
    {
        genres genres;
        String imdb_id;
        String original_title;
        String poster_path;
        String countries;
        String release;
        String title;
        String rating;
        credits credits;
        alternative_titles alternative_titles;
    }

    class TMDb
    {
        movie_detail det;
    }
}
