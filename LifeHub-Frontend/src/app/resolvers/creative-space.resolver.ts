import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { CreativeSpace } from '../models/creative-space.model';
import { CreativeSpaceService } from '../services/creative-space.service';

export const creativeSpaceResolver: ResolveFn<CreativeSpace> = (route) => {
  const creativeSpaceService = inject(CreativeSpaceService);
  const spaceId = Number(route.paramMap.get('id'));

  return creativeSpaceService.getSpace(spaceId);
};