import { isNgTemplate } from '@angular/compiler';
import { TestBed } from '@angular/core/testing';

import { ChessHubService } from './chess-hub.service';

describe('ChessHubService', () => {
  let service: ChessHubService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ChessHubService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
