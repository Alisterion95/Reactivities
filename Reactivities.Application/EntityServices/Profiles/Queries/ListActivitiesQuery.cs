﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Reactivities.Application.EntityServices.Profiles;
using Reactivities.Application.Exceptions;
using Reactivities.Persistence;

namespace Application.Profiles
{
    public class ListActivities
    {
        public class Query : IRequest<List<UserActivityDto>>
        {
            public string Username { get; set; }
            public string Predicate { get; set; }
        }

        public class Handler : IRequestHandler<Query, List<UserActivityDto>>
        {
            private readonly ReactivitiesDbContext _context;
            public Handler(ReactivitiesDbContext context)
            {
                _context = context;
            }

            public async Task<List<UserActivityDto>> Handle(Query request,
                CancellationToken cancellationToken)
            {
                var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == request.Username, cancellationToken);

                if (user == null)
                    throw new RestException(HttpStatusCode.NotFound, new { User = "Not found" });

                var queryable = user.UserActivities
                    .OrderBy(a => a.Activity.Date)
                    .AsQueryable();

                switch (request.Predicate)
                {
                    case "past":
                        queryable = queryable.Where(a => a.Activity.Date <= DateTime.Now);
                        break;
                    case "hosting":
                        queryable = queryable.Where(a => a.IsHost);
                        break;
                    default:
                        queryable = queryable.Where(a => a.Activity.Date >= DateTime.Now);
                        break;
                }

                var activities = queryable.ToList();
                var activitiesToReturn = new List<UserActivityDto>();

                foreach (var activity in activities)
                {
                    var userActivity = new UserActivityDto
                    {
                        Id = activity.Activity.Id,
                        Title = activity.Activity.Name,
                        Category = activity.Activity.Category,
                        Date = activity.Activity.Date
                    };

                    activitiesToReturn.Add(userActivity);
                }

                return activitiesToReturn;
            }
        }
    }
}